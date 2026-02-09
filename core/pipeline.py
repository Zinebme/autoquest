import os
import cv2
from typing import List, Callable, Optional
from core.models import Project, Questionnaire, Field
from core.ai_engine import AIEngine
from core.image_utils import redact_phi

class ExtractionPipeline:
    def __init__(self, ai_engine: AIEngine):
        self.ai_engine = ai_engine

    def run(self, project: Project, image_paths: List[str], progress_callback: Optional[Callable] = None):
        """
        Runs the full extraction pipeline on a list of images.
        """
        # Group images into questionnaires
        n = project.pages_per_questionnaire
        image_groups = [image_paths[i:i + n] for i in range(0, len(image_paths), n)]

        results = []
        for idx, group in enumerate(image_groups):
            if progress_callback:
                progress_callback(idx, len(image_groups), "Processing Questionnaire...")

            # Load original images
            images = [cv2.imread(p) for p in group]

            # Create a Questionnaire object
            q_id = f"Patient_{idx + 1}"
            q = Questionnaire(id=q_id, image_paths=group)

            # 1. Redact PHI for cloud extraction
            # We combine all pages for the cloud AI if it supports multi-image,
            # or send them one by one. GPT-4o supports multiple images in one call.
            redacted_images = []
            for p_idx, img in enumerate(images):
                if img is None: continue
                redacted = redact_phi(img, project.fields, p_idx)
                redacted_images.append(redacted)

            # 2. Extract non-PHI fields via Cloud AI
            cloud_data = {}
            for p_idx, red_img in enumerate(redacted_images):
                page_fields = [f for f in project.fields if f.page_index == p_idx and not f.is_phi and f.is_selected]
                if page_fields:
                    extracted = self.ai_engine.extract_fields(red_img, page_fields)
                    cloud_data.update(extracted)

            # 3. Extract PHI fields via Local AI
            from core.image_utils import denormalize_bbox
            local_data = {}
            for field in project.fields:
                if field.is_phi and field.is_selected and field.bbox_norm:
                    p_idx = field.page_index
                    if p_idx < len(images) and images[p_idx] is not None:
                        img_h, img_w = images[p_idx].shape[:2]
                        bbox_pixel = denormalize_bbox(field.bbox_norm, (img_h, img_w))
                        val = self.ai_engine.extract_local(images[p_idx], bbox_pixel)
                        local_data[field.name] = val

            # Merge
            q.data = {**cloud_data, **local_data}
            results.append(q)

        project.questionnaires = results
        return results
