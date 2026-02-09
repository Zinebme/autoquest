import cv2
import numpy as np
from typing import List, Any

def denormalize_bbox(bbox_norm: List[int], img_shape: tuple) -> tuple:
    """
    Converts [ymin, xmin, ymax, xmax] (0-1000) to (x, y, w, h) in pixels.
    """
    h_img, w_img = img_shape[:2]
    ymin, xmin, ymax, xmax = bbox_norm

    x = int(xmin * w_img / 1000)
    y = int(ymin * h_img / 1000)
    w = int((xmax - xmin) * w_img / 1000)
    height = int((ymax - ymin) * h_img / 1000)

    return (x, y, w, height)

def normalize_bbox(bbox_pixel: tuple, img_shape: tuple) -> List[int]:
    """
    Converts (x, y, w, h) in pixels to [ymin, xmin, ymax, xmax] (0-1000).
    """
    h_img, w_img = img_shape[:2]
    x, y, w, height = bbox_pixel

    xmin = int(x * 1000 / w_img)
    ymin = int(y * 1000 / h_img)
    xmax = int((x + w) * 1000 / w_img)
    ymax = int((y + height) * 1000 / h_img)

    return [ymin, xmin, ymax, xmax]

def redact_phi(image: np.ndarray, fields: List[Any], page_index: int) -> np.ndarray:
    """
    Blacks out PHI fields in the image for the given page_index.
    Uses bbox_norm for scaling.
    """
    redacted_img = image.copy()
    h_img, w_img = image.shape[:2]
    for field in fields:
        if field.is_phi and field.page_index == page_index and field.bbox_norm:
            x, y, w, h = denormalize_bbox(field.bbox_norm, (h_img, w_img))
            cv2.rectangle(redacted_img, (x, y), (x + w, y + h), (0, 0, 0), -1)
    return redacted_img

def draw_highlights(image: np.ndarray, fields: List[Any], page_index: int, target_field_name: str = None) -> np.ndarray:
    """
    Draws red boxes around fields for verification.
    Uses bbox_norm for scaling.
    """
    highlighted_img = image.copy()
    h_img, w_img = image.shape[:2]
    for field in fields:
        if field.page_index == page_index and field.bbox_norm:
            x, y, w, h = denormalize_bbox(field.bbox_norm, (h_img, w_img))
            color = (0, 0, 255) # Red
            thickness = 2
            if target_field_name and field.name == target_field_name:
                thickness = 5
            cv2.rectangle(highlighted_img, (x, y), (x + w, y + h), color, thickness)
            cv2.putText(highlighted_img, field.name, (x, y - 5), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 1)
    return highlighted_img
