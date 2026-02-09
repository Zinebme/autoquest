from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QTableWidget,
                             QTableWidgetItem, QPushButton, QLabel, QHeaderView,
                             QScrollArea, QFileDialog)
from PyQt6.QtGui import QPixmap, QImage
from PyQt6.QtCore import Qt
import pandas as pd
import cv2
from core.image_utils import draw_highlights

class VerificationTable(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.layout = QVBoxLayout(self)

        # Split layout: Table on top, Image preview below (or side-by-side)
        self.upper_layout = QHBoxLayout()

        self.table = QTableWidget()
        self.upper_layout.addWidget(self.table, 2)

        # Image Preview
        self.preview_area = QScrollArea()
        self.image_label = QLabel("Select a cell to view source")
        self.image_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.preview_area.setWidget(self.image_label)
        self.preview_area.setWidgetResizable(True)
        self.upper_layout.addWidget(self.preview_area, 1)

        self.layout.addLayout(self.upper_layout)

        # Actions
        self.actions_layout = QHBoxLayout()
        self.btn_save = QPushButton("Save Project (.json)")
        self.btn_save.clicked.connect(self.save_project)
        self.btn_export = QPushButton("Export to Excel (.xlsx)")
        self.btn_export.clicked.connect(self.export_to_excel)

        self.actions_layout.addWidget(self.btn_save)
        self.actions_layout.addWidget(self.btn_export)
        self.layout.addLayout(self.actions_layout)

        self.current_project = None
        self.table.cellClicked.connect(self.on_cell_clicked)

    def load_project_data(self, project):
        self.current_project = project
        fields = project.fields
        questionnaires = project.questionnaires

        self.table.setColumnCount(len(fields))
        self.table.setHorizontalHeaderLabels([f.name for f in fields])
        self.table.setRowCount(len(questionnaires))
        self.table.setVerticalHeaderLabels([q.id for q in questionnaires])

        for row, q in enumerate(questionnaires):
            for col, f in enumerate(fields):
                val = q.data.get(f.name, "")
                self.table.setItem(row, col, QTableWidgetItem(str(val)))

    def on_cell_clicked(self, row, col):
        if not self.current_project:
            return

        field = self.current_project.fields[col]
        questionnaire = self.current_project.questionnaires[row]

        # Load image for the field's page
        page_idx = field.page_index
        if page_idx < len(questionnaire.image_paths):
            img_path = questionnaire.image_paths[page_idx]
            img = cv2.imread(img_path)
            if img is not None:
                # Draw highlight for this specific field
                highlighted = draw_highlights(img, self.current_project.fields, page_idx, target_field_name=field.name)

                # Convert to QImage
                height, width, channel = highlighted.shape
                bytes_per_line = 3 * width
                q_img = QImage(highlighted.data, width, height, bytes_per_line, QImage.Format.Format_BGR888)
                self.image_label.setPixmap(QPixmap.fromImage(q_img).scaled(
                    self.image_label.size(), Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))

    def save_project(self):
        if not self.current_project:
            return
        path, _ = QFileDialog.getSaveFileName(self, "Save Project", "", "JSON Files (*.json)")
        if path:
            if not path.endswith(".json"):
                path += ".json"
            self.current_project.save(path)

    def export_to_excel(self):
        if not self.current_project:
            return

        data = []
        for q in self.current_project.questionnaires:
            row_dict = {"Patient ID": q.id}
            row_dict.update(q.data)
            data.append(row_dict)

        df = pd.DataFrame(data)

        path, _ = QFileDialog.getSaveFileName(self, "Save Excel", "", "Excel Files (*.xlsx)")
        if path:
            if not path.endswith(".xlsx"):
                path += ".xlsx"
            df.to_excel(path, index=False)
