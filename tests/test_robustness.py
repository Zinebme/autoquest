import unittest
from unittest.mock import MagicMock
import numpy as np
import cv2
import os
from core.models import Project, Field
from core.pipeline import ExtractionPipeline
from core.image_utils import normalize_bbox, denormalize_bbox

class TestRobustness(unittest.TestCase):
    def setUp(self):
        self.mock_ai = MagicMock()
        self.pipeline = ExtractionPipeline(self.mock_ai)

    def test_scaling_logic(self):
        img_template_shape = (1000, 1000)
        bbox_pixel = (100, 100, 200, 50)
        bbox_norm = normalize_bbox(bbox_pixel, img_template_shape)
        new_img_shape = (2000, 2000)
        new_bbox_pixel = denormalize_bbox(bbox_norm, new_img_shape)
        self.assertEqual(new_bbox_pixel, (200, 200, 400, 100))

    def test_pipeline_different_resolutions(self):
        project = Project(name="Robust", pages_per_questionnaire=1)
        project.fields = [
            Field(name="PHI_Field", is_phi=True, bbox_norm=[400, 400, 600, 600], page_index=0)
        ]
        img1_path = "img1.jpg"
        img2_path = "img2.jpg"
        cv2.imwrite(img1_path, np.zeros((500, 500, 3), dtype=np.uint8))
        cv2.imwrite(img2_path, np.zeros((1000, 1000, 3), dtype=np.uint8))
        self.mock_ai.extract_local.side_effect = ["Val1", "Val2"]
        results = self.pipeline.run(project, [img1_path, img2_path])
        self.assertEqual(len(results), 2)
        self.assertEqual(results[0].data["PHI_Field"], "Val1")
        self.assertEqual(results[1].data["PHI_Field"], "Val2")
        calls = self.mock_ai.extract_local.call_args_list
        self.assertEqual(calls[0][0][1], (200, 200, 100, 100))
        self.assertEqual(calls[1][0][1], (400, 400, 200, 200))
        for f in [img1_path, img2_path]:
            if os.path.exists(f): os.remove(f)

if __name__ == "__main__":
    unittest.main()
