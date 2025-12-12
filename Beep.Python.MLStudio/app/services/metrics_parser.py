"""
Metrics Parser
Extracts metrics from workflow execution output
"""
import re
import json
import logging
from typing import Dict, Any, Optional

logger = logging.getLogger(__name__)


class MetricsParser:
    """Parse metrics from execution stdout"""
    
    def parse_metrics(self, stdout: str) -> Optional[Dict[str, Any]]:
        """
        Parse metrics from stdout text
        
        Returns:
            Dictionary of parsed metrics or None if no metrics found
        """
        if not stdout:
            return None
        
        metrics = {}
        
        # Classification metrics
        metrics.update(self._parse_classification_metrics(stdout))
        
        # Regression metrics
        metrics.update(self._parse_regression_metrics(stdout))
        
        # General metrics
        metrics.update(self._parse_general_metrics(stdout))
        
        # Confusion matrix
        confusion_matrix = self._parse_confusion_matrix(stdout)
        if confusion_matrix:
            metrics['confusion_matrix'] = confusion_matrix
        
        return metrics if metrics else None
    
    def _parse_classification_metrics(self, text: str) -> Dict[str, Any]:
        """Parse classification metrics"""
        metrics = {}
        
        # Accuracy
        accuracy_match = re.search(r'accuracy[:\s=]+([\d.]+)', text, re.IGNORECASE)
        if accuracy_match:
            try:
                metrics['accuracy'] = float(accuracy_match.group(1))
            except:
                pass
        
        # Precision
        precision_match = re.search(r'precision[:\s=]+([\d.]+)', text, re.IGNORECASE)
        if precision_match:
            try:
                metrics['precision'] = float(precision_match.group(1))
            except:
                pass
        
        # Recall
        recall_match = re.search(r'recall[:\s=]+([\d.]+)', text, re.IGNORECASE)
        if recall_match:
            try:
                metrics['recall'] = float(recall_match.group(1))
            except:
                pass
        
        # F1 Score
        f1_match = re.search(r'f1[_\s-]?score[:\s=]+([\d.]+)', text, re.IGNORECASE)
        if f1_match:
            try:
                metrics['f1_score'] = float(f1_match.group(1))
            except:
                pass
        
        # Classification report parsing
        report_match = re.search(
            r'precision\s+recall\s+f1-score\s+support.*?\n(.*?)\n\n',
            text,
            re.DOTALL | re.IGNORECASE
        )
        if report_match:
            # Try to extract per-class metrics
            lines = report_match.group(1).strip().split('\n')
            class_metrics = {}
            for line in lines:
                if 'avg' in line.lower() or 'macro' in line.lower() or 'weighted' in line.lower():
                    parts = line.split()
                    if len(parts) >= 4:
                        metric_type = parts[0].lower()
                        try:
                            class_metrics[f'{metric_type}_precision'] = float(parts[1])
                            class_metrics[f'{metric_type}_recall'] = float(parts[2])
                            class_metrics[f'{metric_type}_f1'] = float(parts[3])
                        except:
                            pass
            if class_metrics:
                metrics.update(class_metrics)
        
        return metrics
    
    def _parse_regression_metrics(self, text: str) -> Dict[str, Any]:
        """Parse regression metrics"""
        metrics = {}
        
        # MSE
        mse_match = re.search(r'mse[:\s=]+([\d.e+-]+)', text, re.IGNORECASE)
        if mse_match:
            try:
                metrics['mse'] = float(mse_match.group(1))
            except:
                pass
        
        # RMSE
        rmse_match = re.search(r'rmse[:\s=]+([\d.e+-]+)', text, re.IGNORECASE)
        if rmse_match:
            try:
                metrics['rmse'] = float(rmse_match.group(1))
            except:
                pass
        
        # MAE
        mae_match = re.search(r'mae[:\s=]+([\d.e+-]+)', text, re.IGNORECASE)
        if mae_match:
            try:
                metrics['mae'] = float(mae_match.group(1))
            except:
                pass
        
        # R² Score
        r2_match = re.search(r'r2[_\s-]?score[:\s=]+([\d.e+-]+)', text, re.IGNORECASE)
        if r2_match:
            try:
                metrics['r2_score'] = float(r2_match.group(1))
            except:
                pass
        
        # R² Score (alternative format)
        r2_match2 = re.search(r'r\^2[:\s=]+([\d.e+-]+)', text, re.IGNORECASE)
        if r2_match2:
            try:
                metrics['r2_score'] = float(r2_match2.group(1))
            except:
                pass
        
        return metrics
    
    def _parse_general_metrics(self, text: str) -> Dict[str, Any]:
        """Parse general metrics"""
        metrics = {}
        
        # Loss
        loss_match = re.search(r'loss[:\s=]+([\d.e+-]+)', text, re.IGNORECASE)
        if loss_match:
            try:
                metrics['loss'] = float(loss_match.group(1))
            except:
                pass
        
        # Training/test accuracy
        train_acc_match = re.search(r'train[_\s-]?accuracy[:\s=]+([\d.]+)', text, re.IGNORECASE)
        if train_acc_match:
            try:
                metrics['train_accuracy'] = float(train_acc_match.group(1))
            except:
                pass
        
        test_acc_match = re.search(r'test[_\s-]?accuracy[:\s=]+([\d.]+)', text, re.IGNORECASE)
        if test_acc_match:
            try:
                metrics['test_accuracy'] = float(test_acc_match.group(1))
            except:
                pass
        
        return metrics
    
    def _parse_confusion_matrix(self, text: str) -> Optional[list]:
        """Parse confusion matrix from text"""
        # Look for array-like patterns
        # Example: [[100, 5], [10, 85]]
        matrix_match = re.search(r'\[[\s\n]*\[[\s\n]*(\d+)[\s\n]*,[\s\n]*(\d+)[\s\n]*\][\s\n]*,[\s\n]*\[[\s\n]*(\d+)[\s\n]*,[\s\n]*(\d+)[\s\n]*\]', text)
        if matrix_match:
            try:
                return [
                    [int(matrix_match.group(1)), int(matrix_match.group(2))],
                    [int(matrix_match.group(3)), int(matrix_match.group(4))]
                ]
            except:
                pass
        
        # Look for tabular format
        # Example:
        #   0   1
        # 0 100 5
        # 1 10  85
        lines = text.split('\n')
        for i, line in enumerate(lines):
            if re.search(r'\d+\s+\d+', line) and i + 1 < len(lines):
                next_line = lines[i + 1]
                if re.search(r'\d+\s+\d+', next_line):
                    try:
                        row1 = [int(x) for x in line.split() if x.isdigit()]
                        row2 = [int(x) for x in next_line.split() if x.isdigit()]
                        if len(row1) == 2 and len(row2) == 2:
                            return [row1, row2]
                    except:
                        pass
        
        return None

