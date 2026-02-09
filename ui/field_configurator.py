from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QPushButton,
                             QTableWidget, QTableWidgetItem, QHeaderView,
                             QLabel, QCheckBox)
from PyQt6.QtCore import Qt

class FieldConfigurator(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.layout = QVBoxLayout(self)

        self.layout.addWidget(QLabel("Configure Fields (Template)"))

        # Actions
        self.btn_layout = QHBoxLayout()
        self.btn_detect = QPushButton("Auto-detect Fields")
        self.btn_add = QPushButton("Add Field")
        self.btn_delete = QPushButton("Delete Selected")
        self.btn_select_all = QPushButton("Select All")
        self.btn_unselect_all = QPushButton("Unselect All")
        self.btn_clear = QPushButton("Clear All")

        self.btn_layout.addWidget(self.btn_detect)
        self.btn_layout.addWidget(self.btn_add)
        self.btn_layout.addWidget(self.btn_delete)
        self.btn_layout.addWidget(self.btn_select_all)
        self.btn_layout.addWidget(self.btn_unselect_all)
        self.btn_layout.addWidget(self.btn_clear)
        self.layout.addLayout(self.btn_layout)

        # Table
        self.table = QTableWidget(0, 5)
        self.table.setHorizontalHeaderLabels(["Selected", "Field Name", "Description", "Is PHI?", "Page"])
        self.table.horizontalHeader().setSectionResizeMode(QHeaderView.ResizeMode.Stretch)
        self.layout.addWidget(self.table)

        # Connect new buttons
        self.btn_select_all.clicked.connect(lambda: self.set_all_selected(True))
        self.btn_unselect_all.clicked.connect(lambda: self.set_all_selected(False))
        self.btn_delete.clicked.connect(self.delete_selected)
        self.btn_clear.clicked.connect(lambda: self.table.setRowCount(0))

        # Confirm
        self.btn_confirm = QPushButton("Confirm and Start Extraction")
        self.btn_confirm.setStyleSheet("background-color: green; color: white; font-weight: bold;")
        self.layout.addWidget(self.btn_confirm)

    def add_field_to_table(self, name="", desc="", is_phi=False, page=0, is_selected=True):
        row = self.table.rowCount()
        self.table.insertRow(row)

        # Selected Checkbox
        sel_check = QCheckBox()
        sel_check.setChecked(is_selected)
        sel_widget = QWidget()
        sel_layout = QHBoxLayout(sel_widget)
        sel_layout.addWidget(sel_check)
        sel_layout.setAlignment(Qt.AlignmentFlag.AlignCenter)
        sel_layout.setContentsMargins(0,0,0,0)
        self.table.setCellWidget(row, 0, sel_widget)

        self.table.setItem(row, 1, QTableWidgetItem(name))
        self.table.setItem(row, 2, QTableWidgetItem(desc))

        # PHI Checkbox
        phi_check = QCheckBox()
        phi_check.setChecked(is_phi)
        phi_widget = QWidget()
        phi_layout = QHBoxLayout(phi_widget)
        phi_layout.addWidget(phi_check)
        phi_layout.setAlignment(Qt.AlignmentFlag.AlignCenter)
        phi_layout.setContentsMargins(0,0,0,0)
        self.table.setCellWidget(row, 3, phi_widget)

        self.table.setItem(row, 4, QTableWidgetItem(str(page)))

    def set_all_selected(self, state):
        for row in range(self.table.rowCount()):
            sel_widget = self.table.cellWidget(row, 0)
            sel_widget.findChild(QCheckBox).setChecked(state)

    def delete_selected(self):
        rows_to_delete = []
        for row in range(self.table.rowCount()):
            if self.table.item(row, 1).isSelected(): # Use standard selection or checkbox
                rows_to_delete.append(row)

        for row in reversed(rows_to_delete):
            self.table.removeRow(row)

    def get_fields(self):
        fields_data = []
        for row in range(self.table.rowCount()):
            sel_widget = self.table.cellWidget(row, 0)
            is_selected = sel_widget.findChild(QCheckBox).isChecked()

            name = self.table.item(row, 1).text()
            desc = self.table.item(row, 2).text()

            phi_widget = self.table.cellWidget(row, 3)
            is_phi = phi_widget.findChild(QCheckBox).isChecked()

            page = int(self.table.item(row, 4).text())

            fields_data.append({
                "name": name,
                "description": desc,
                "is_phi": is_phi,
                "is_selected": is_selected,
                "page_index": page
            })
        return fields_data
