import sys
import os
from PyQt6.QtWidgets import QApplication, QMainWindow, QStackedWidget, QMessageBox, QFileDialog
from PyQt6.QtCore import QThread, pyqtSignal, QObject
from core.models import Project, Field
from core.ai_engine import AIEngine
from core.pipeline import ExtractionPipeline
from core.image_utils import denormalize_bbox
from ui.project_wizard import ProjectWizard
from ui.field_configurator import FieldConfigurator
from ui.verification_table import VerificationTable
import cv2

class ExtractionWorker(QObject):
    finished = pyqtSignal(list)
    progress = pyqtSignal(int, int, str)
    error = pyqtSignal(str)

    def __init__(self, pipeline, project, image_paths):
        super().__init__()
        self.pipeline = pipeline
        self.project = project
        self.image_paths = image_paths

    def run(self):
        try:
            results = self.pipeline.run(
                self.project,
                self.image_paths,
                progress_callback=self.progress.emit
            )
            self.finished.emit(results)
        except Exception as e:
            self.error.emit(str(e))

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Intelligent Document Processor - Medical Research")
        self.resize(1200, 800)

        self.stack = QStackedWidget()
        self.setCentralWidget(self.stack)

        self.ai_engine = AIEngine()
        self.pipeline = ExtractionPipeline(self.ai_engine)
        self.current_project = None

        # Pages
        self.wizard = ProjectWizard()
        self.field_config = FieldConfigurator()
        self.verification = VerificationTable()

        self.stack.addWidget(self.wizard)
        self.stack.addWidget(self.field_config)
        self.stack.addWidget(self.verification)

        # Connections
        self.wizard.btn_create.clicked.connect(self.create_project)
        self.wizard.btn_open.clicked.connect(self.open_project)
        self.field_config.btn_detect.clicked.connect(self.auto_detect_fields)
        self.field_config.btn_confirm.clicked.connect(self.start_extraction)

    def open_project(self):
        path, _ = QFileDialog.getOpenFileName(self, "Open Project", "", "JSON Files (*.json)")
        if path:
            try:
                self.current_project = Project.load(path)
                # We also need the image paths.
                # For now assume they are in the same directory or stored in the project.
                # Project model should probably store the original image paths.
                # In my models.py, Questionnaire stores image_paths, but Project doesn't store the "pool".
                # I'll add them to Project later if needed.
                self.all_image_paths = []
                for q in self.current_project.questionnaires:
                    self.all_image_paths.extend(q.image_paths)

                # Load fields into UI
                self.field_config.table.setRowCount(0)
                for f in self.current_project.fields:
                    self.field_config.add_field_to_table(f.name, f.description, f.is_phi, f.page_index, f.is_selected)

                self.stack.setCurrentWidget(self.field_config)
            except Exception as e:
                QMessageBox.critical(self, "Error", f"Failed to load project: {str(e)}")

    def create_project(self):
        data = self.wizard.get_project_data()
        if not data["name"]:
            QMessageBox.warning(self, "Error", "Project name is required.")
            return
        if not data["image_paths"]:
            QMessageBox.warning(self, "Error", "Please add some images.")
            return

        self.current_project = Project(
            name=data["name"],
            pages_per_questionnaire=data["pages_per_questionnaire"]
        )
        self.all_image_paths = data["image_paths"]

        # Move to field configuration using the first image as template
        self.stack.setCurrentWidget(self.field_config)

    def auto_detect_fields(self):
        if not self.all_image_paths:
            return

        template_img_path = self.all_image_paths[0]
        try:
            raw_fields = self.ai_engine.detect_fields(template_img_path)

            # Load image to denormalize bboxes
            img = cv2.imread(template_img_path)
            h, w = img.shape[:2]

            self.field_config.table.setRowCount(0)
            for f_data in raw_fields:
                name = f_data.get("name", "Field")
                desc = f_data.get("description", "")
                is_phi = f_data.get("is_phi", False)
                bbox_norm = f_data.get("bbox")

                # Convert bbox to pixels
                bbox_pixel = denormalize_bbox(bbox_norm, (h, w)) if bbox_norm else None

                # We store them in the project fields later, for now just UI
                self.field_config.add_field_to_table(name, desc, is_phi, 0)

                # Add to project model
                field_obj = Field(
                    name=name,
                    description=desc,
                    is_phi=is_phi,
                    bbox_norm=bbox_norm,
                    page_index=0
                )
                self.current_project.fields.append(field_obj)

        except Exception as e:
            QMessageBox.critical(self, "AI Error", f"Failed to detect fields: {str(e)}")

    def start_extraction(self):
        # Update project fields from UI
        fields_data = self.field_config.get_fields()

        # In a real app we'd merge by name/id to keep bbox_norm
        # For now, let's just use the current fields and update flags
        for i, fd in enumerate(fields_data):
            if i < len(self.current_project.fields):
                f = self.current_project.fields[i]
                f.name = fd["name"]
                f.description = fd["description"]
                f.is_phi = fd["is_phi"]
                f.is_selected = fd["is_selected"]
                f.page_index = fd["page_index"]

        # Show progress dialog
        self.progress_msg = QMessageBox(self)
        self.progress_msg.setWindowTitle("Extraction")
        self.progress_msg.setText("Starting extraction...")
        self.progress_msg.setStandardButtons(QMessageBox.StandardButton.NoButton)
        self.progress_msg.show()

        # Threaded execution
        self.thread = QThread()
        self.worker = ExtractionWorker(self.pipeline, self.current_project, self.all_image_paths)
        self.worker.moveToThread(self.thread)

        self.thread.started.connect(self.worker.run)
        self.worker.finished.connect(self.on_extraction_finished)
        self.worker.progress.connect(self.on_extraction_progress)
        self.worker.error.connect(self.on_extraction_error)

        self.thread.start()

    def on_extraction_progress(self, current, total, message):
        self.progress_msg.setText(f"[{current}/{total}] {message}")

    def on_extraction_error(self, error_msg):
        self.thread.quit()
        self.progress_msg.close()
        QMessageBox.critical(self, "Extraction Error", error_msg)

    def on_extraction_finished(self, results):
        self.thread.quit()
        self.progress_msg.close()
        self.verification.load_project_data(self.current_project)
        self.stack.setCurrentWidget(self.verification)

if __name__ == "__main__":
    app = QApplication(sys.argv)
    window = MainWindow()
    window.show()
    sys.exit(app.exec())
