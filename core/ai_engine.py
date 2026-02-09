import os
import base64
import json
import cv2
import easyocr
import numpy as np
from openai import OpenAI
from typing import List, Dict, Any, Optional

class AIEngine:
    def __init__(self, api_key: Optional[str] = None):
        self.api_key = api_key or os.environ.get("OPENAI_API_KEY")
        if self.api_key:
            self.client = OpenAI(api_key=self.api_key)
        else:
            self.client = None

        # Initialize EasyOCR for local extraction (English and French)
        self.reader = easyocr.Reader(['en', 'fr'])

    def _encode_image(self, image_path: str) -> str:
        with open(image_path, "rb") as image_file:
            return base64.b64encode(image_file.read()).decode('utf-8')

    def _encode_cv2_image(self, image: np.ndarray) -> str:
        _, buffer = cv2.imencode(".jpg", image)
        return base64.b64encode(buffer).decode('utf-8')

    def detect_fields(self, image_path: str) -> List[Dict[str, Any]]:
        """
        Uses GPT-4o-mini to detect fields in a questionnaire template.
        Returns a list of fields with names and approximate bounding boxes if possible.
        """
        if not self.client:
            raise ValueError("OpenAI API Key not set.")

        base64_image = self._encode_image(image_path)

        prompt = (
            "You are an expert at document processing. Analyze this medical research form and list all fields "
            "that should be extracted. For each field, provide a name, a short description, and if possible, "
            "the approximate location in normalized coordinates [ymin, xmin, ymax, xmax] (0-1000). "
            "Identify if a field likely contains PHI (Protected Health Information) like patient name, address, or birth date. "
            "Respond ONLY in JSON format: {\"fields\": [{\"name\": \"...\", \"description\": \"...\", \"bbox\": [ymin, xmin, ymax, xmax], \"is_phi\": true/false}]}"
        )

        response = self.client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "user",
                    "content": [
                        {"type": "text", "text": prompt},
                        {"type": "image_url", "image_url": {"url": f"data:image/jpeg;base64,{base64_image}"}}
                    ],
                }
            ],
            response_format={"type": "json_object"}
        )

        result = json.loads(response.choices[0].message.content)
        return result.get("fields", [])

    def extract_fields(self, image: np.ndarray, fields: List[Any]) -> Dict[str, Any]:
        """
        Uses GPT-4o-mini to extract non-PHI fields from a (possibly redacted) image.
        """
        if not self.client:
            raise ValueError("OpenAI API Key not set.")

        base64_image = self._encode_cv2_image(image)

        field_list_str = "\n".join([f"- {f.name}: {f.description}" for f in fields if not f.is_phi])

        prompt = (
            "Extract the following fields from the image. If a field is blank, return null. "
            "Respond ONLY in JSON format where keys are field names and values are the extracted text. "
            "Fields to extract:\n" + field_list_str
        )

        response = self.client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "user",
                    "content": [
                        {"type": "text", "text": prompt},
                        {"type": "image_url", "image_url": {"url": f"data:image/jpeg;base64,{base64_image}"}}
                    ],
                }
            ],
            response_format={"type": "json_object"}
        )

        return json.loads(response.choices[0].message.content)

    def extract_local(self, image: np.ndarray, bbox: tuple) -> str:
        """
        Uses EasyOCR to extract text from a specific region (bbox: x, y, w, h).
        """
        x, y, w, h = bbox
        roi = image[y:y+h, x:x+w]
        if roi.size == 0:
            return ""

        results = self.reader.readtext(roi)
        text = " ".join([res[1] for res in results])
        return text.strip()
