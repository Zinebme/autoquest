from dataclasses import dataclass, field
from typing import List, Dict, Any, Optional
import json

@dataclass
class Field:
    name: str
    bbox_norm: Optional[List[int]] = None  # [ymin, xmin, ymax, xmax] (0-1000)
    is_phi: bool = False
    is_selected: bool = True
    page_index: int = 0
    description: str = ""

    def to_dict(self):
        return {
            "name": self.name,
            "bbox_norm": self.bbox_norm,
            "is_phi": self.is_phi,
            "is_selected": self.is_selected,
            "page_index": self.page_index,
            "description": self.description
        }

    @classmethod
    def from_dict(cls, data):
        return cls(**data)

@dataclass
class Questionnaire:
    id: str
    image_paths: List[str]
    data: Dict[str, Any] = field(default_factory=dict)

    def to_dict(self):
        return {
            "id": self.id,
            "image_paths": self.image_paths,
            "data": self.data
        }

    @classmethod
    def from_dict(cls, data):
        return cls(**data)

@dataclass
class Project:
    name: str
    pages_per_questionnaire: int = 1
    fields: List[Field] = field(default_factory=list)
    questionnaires: List[Questionnaire] = field(default_factory=list)

    def to_dict(self):
        return {
            "name": self.name,
            "pages_per_questionnaire": self.pages_per_questionnaire,
            "fields": [f.to_dict() for f in self.fields],
            "questionnaires": [q.to_dict() for q in self.questionnaires]
        }

    @classmethod
    def from_dict(cls, data):
        fields = [Field.from_dict(f) for f in data.get("fields", [])]
        questionnaires = [Questionnaire.from_dict(q) for q in data.get("questionnaires", [])]
        return cls(
            name=data["name"],
            pages_per_questionnaire=data.get("pages_per_questionnaire", 1),
            fields=fields,
            questionnaires=questionnaires
        )

    def save(self, filepath):
        with open(filepath, 'w') as f:
            json.dump(self.to_dict(), f, indent=4)

    @classmethod
    def load(cls, filepath):
        with open(filepath, 'r') as f:
            data = json.load(f)
        return cls.from_dict(data)
