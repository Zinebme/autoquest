from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                             QLineEdit, QPushButton, QFileDialog, QSpinBox,
                             QMessageBox, QListWidget)
import os

class ProjectWizard(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Project Setup")
        self.layout = QVBoxLayout(self)

        # Project Name
        self.name_layout = QHBoxLayout()
        self.name_layout.addWidget(QLabel("Project Name:"))
        self.name_input = QLineEdit("My Medical Research")
        self.name_layout.addWidget(self.name_input)
        self.layout.addLayout(self.name_layout)

        # Pages per Questionnaire
        self.pages_layout = QHBoxLayout()
        self.pages_layout.addWidget(QLabel("Pages per Questionnaire:"))
        self.pages_spin = QSpinBox()
        self.pages_spin.setMinimum(1)
        self.pages_spin.setValue(1)
        self.pages_layout.addWidget(self.pages_spin)
        self.layout.addLayout(self.pages_layout)

        # Image Selection
        self.layout.addWidget(QLabel("Select Images:"))
        self.image_list = QListWidget()
        self.layout.addWidget(self.image_list)

        self.btn_add_images = QPushButton("Add Images")
        self.btn_add_images.clicked.connect(self.add_images)
        self.layout.addWidget(self.btn_add_images)

        # Action Buttons
        self.actions_layout = QHBoxLayout()
        self.btn_create = QPushButton("Create Project")
        self.btn_open = QPushButton("Open Existing")
        self.actions_layout.addWidget(self.btn_create)
        self.actions_layout.addWidget(self.btn_open)
        self.layout.addLayout(self.actions_layout)

        self.selected_images = []

    def add_images(self):
        files, _ = QFileDialog.getOpenFileNames(self, "Select Images", "", "Image Files (*.png *.jpg *.jpeg *.bmp *.tiff)")
        if files:
            self.selected_images.extend(files)
            self.image_list.addItems(files)

    def get_project_data(self):
        return {
            "name": self.name_input.text(),
            "pages_per_questionnaire": self.pages_spin.value(),
            "image_paths": self.selected_images
        }
