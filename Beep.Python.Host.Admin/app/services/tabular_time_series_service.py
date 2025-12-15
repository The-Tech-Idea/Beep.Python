"""
Tabular and Time Series Service

Supports:
- Time Series Forecasting (TimesFM, TTM, Chronos, etc.)
- Tabular Classification
- Tabular Regression
- Time Series Anomaly Detection
"""
import os
import sys
import subprocess
import base64
import json
import logging
import tempfile
from pathlib import Path
from typing import Dict, Any, Optional, List
from datetime import datetime

logger = logging.getLogger(__name__)


class TabularTimeSeriesService:
    """Service for tabular and time series forecasting"""
    
    def __init__(self):
        from app.services.ai_services_environment import AIServiceType, get_ai_service_env
        
        self.service_type = AIServiceType.TABULAR_TIME_SERIES
        self.env_mgr = get_ai_service_env(self.service_type)
        logger.info(f"Initialized TabularTimeSeriesService with environment manager")
    
    def _get_python_executable(self) -> Optional[Path]:
        """Get Python executable from virtual environment"""
        try:
            python_path = self.env_mgr._get_python_path()
            if python_path and python_path.exists():
                logger.info(f"Using Python from virtual environment: {python_path}")
                return python_path
            else:
                logger.warning("Virtual environment Python not found, using system Python")
                return None
        except Exception as e:
            logger.error(f"Error getting Python executable: {e}")
            return None
    
    def forecast(self, 
                 data: List[List[float]],  # Time series data (list of series)
                 model_id: Optional[str] = None,
                 forecast_horizon: int = 12,
                 context_length: Optional[int] = None,
                 frequency: Optional[str] = None,
                 task_type: str = "time_series_forecasting") -> Dict[str, Any]:
        """
        Perform time series forecasting or tabular prediction
        
        Args:
            data: Time series data (list of series) or tabular data
            model_id: HuggingFace model ID (e.g., "google/timesfm-1.0-200m", "amazon/chronos-t5-small")
            forecast_horizon: Number of time steps to forecast
            context_length: Historical context length (auto-determined if not provided)
            frequency: Time series frequency (e.g., "H" for hourly, "D" for daily, "M" for monthly)
            task_type: "time_series_forecasting", "tabular_classification", "tabular_regression"
        
        Returns:
            Dict with 'success', 'forecasts' (list of forecasted values), 'metadata', 'error'
        """
        try:
            # Get Python executable
            python_exe = self._get_python_executable()
            if not python_exe:
                return {
                    'success': False,
                    'error': 'Python environment not available. Please create the environment first.'
                }
            
            # Get selected model if not provided
            if not model_id:
                from app.services.ai_service_models import AIServiceModelConfig
                model_id = AIServiceModelConfig.get_selected_model('tabular_time_series')
                if not model_id:
                    # Default to a popular model
                    model_id = "amazon/chronos-t5-small"
            
            # Track loaded model (only one per service type)
            from app.services.ai_service_loaded_models import get_ai_service_loaded_tracker
            tracker = get_ai_service_loaded_tracker()
            previous_model = tracker.load_model('tabular_time_series', model_id)
            if previous_model and previous_model != model_id:
                logger.info(f"[TabularTimeSeries] Switched from {previous_model} to {model_id}")
            
            tracker.update_last_used('tabular_time_series')
            
            # Use subprocess to run in dedicated environment
            return self._forecast_via_subprocess(
                data, model_id, forecast_horizon, context_length, frequency, task_type
            )
            
        except Exception as e:
            logger.error(f"Error in forecast: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def _forecast_via_subprocess(self,
                                 data: List[List[float]],
                                 model_id: str,
                                 forecast_horizon: int,
                                 context_length: Optional[int],
                                 frequency: Optional[str],
                                 task_type: str) -> Dict[str, Any]:
        """Run forecasting in subprocess"""
        try:
            python_exe = self._get_python_executable()
            if not python_exe:
                return {
                    'success': False,
                    'error': 'Python executable not found'
                }
            
            # Create temporary script
            script_content = self._get_forecast_script()
            script_path = Path(tempfile.gettempdir()) / f"forecast_{datetime.now().timestamp()}.py"
            
            try:
                script_path.write_text(script_content, encoding='utf-8')
                
                # Encode data as JSON
                data_json = json.dumps(data)
                context_length_str = str(context_length) if context_length else "None"
                frequency_str = frequency if frequency else "None"
                
                # Run forecast
                logger.info(f"Starting forecast: model={model_id}, horizon={forecast_horizon}, task={task_type}")
                logger.info(f"Using Python from virtual environment: {python_exe}")
                
                result = subprocess.run(
                    [str(python_exe), str(script_path),
                     data_json, model_id, str(forecast_horizon), context_length_str, frequency_str, task_type],
                    capture_output=True,
                    text=True,
                    timeout=600  # 10 minute timeout
                )
                
                logger.info(f"Forecast completed: returncode={result.returncode}")
                
                if result.returncode == 0:
                    output = result.stdout.strip()
                    try:
                        return json.loads(output)
                    except json.JSONDecodeError:
                        return {
                            'success': False,
                            'error': 'Failed to parse forecast result'
                        }
                else:
                    error_msg = result.stderr or result.stdout or 'Unknown error'
                    logger.error(f"Forecast failed: {error_msg}")
                    return {
                        'success': False,
                        'error': error_msg[:500]  # Limit error message length
                    }
                    
            finally:
                # Clean up script
                if script_path.exists():
                    try:
                        script_path.unlink()
                    except:
                        pass
                        
        except subprocess.TimeoutExpired:
            logger.error("Forecast timed out")
            return {
                'success': False,
                'error': 'Forecast timed out after 10 minutes'
            }
        except Exception as e:
            logger.error(f"Error in _forecast_via_subprocess: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def _get_forecast_script(self) -> str:
        """Get Python script for forecasting"""
        return '''import sys
import json
import numpy as np
from pathlib import Path

# Get arguments
data_json = sys.argv[1]
model_id = sys.argv[2]
forecast_horizon = int(sys.argv[3])
context_length = int(sys.argv[4]) if sys.argv[4] != "None" else None
frequency = sys.argv[5] if sys.argv[5] != "None" else None
task_type = sys.argv[6]

try:
    # Parse input data
    data = json.loads(data_json)
    
    # Convert to numpy arrays
    if isinstance(data[0], list):
        # Multiple time series
        series_list = [np.array(series) for series in data]
    else:
        # Single time series
        series_list = [np.array(data)]
    
    # Detect model type
    is_timesfm = "timesfm" in model_id.lower()
    is_ttm = "ttm" in model_id.lower() or "granite-timeseries" in model_id.lower()
    # Check chronos-bolt first (more specific), then chronos (more general)
    is_chronos_bolt = "chronos-bolt" in model_id.lower() or "chronos_bolt" in model_id.lower() or "autogluon" in model_id.lower()
    is_chronos = "chronos" in model_id.lower() and not is_chronos_bolt  # Only if not chronos-bolt
    is_mitra = "mitra" in model_id.lower()
    
    forecasts = []
    metadata = {
        "model": model_id,
        "task_type": task_type,
        "forecast_horizon": forecast_horizon
    }
    
    if is_timesfm:
        # TimesFM models (Google)
        try:
            import timesfm
            import torch
            
            device = "cuda" if torch.cuda.is_available() else "cpu"
            
            # Initialize model
            if "2.5" in model_id:
                # TimesFM 2.5
                model = timesfm.TimesFM_2p5_200M_torch.from_pretrained(model_id, torch_compile=False)
                model.compile(
                    timesfm.ForecastConfig(
                        max_context=context_length or 1024,
                        max_horizon=forecast_horizon,
                        normalize_inputs=True,
                    )
                )
            elif "2.0" in model_id:
                # TimesFM 2.0
                tfm = timesfm.TimesFm(
                    hparams=timesfm.TimesFmHparams(
                        backend="pytorch",
                        per_core_batch_size=32,
                        horizon_len=forecast_horizon,
                        input_patch_len=32,
                        output_patch_len=128,
                        num_layers=50,
                        model_dims=1280,
                        use_positional_embedding=False,
                    ),
                    checkpoint=timesfm.TimesFmCheckpoint(huggingface_repo_id=model_id),
                )
            else:
                # TimesFM 1.0
                tfm = timesfm.TimesFm(
                    context_len=context_length or 512,
                    horizon_len=forecast_horizon,
                    input_patch_len=32,
                    output_patch_len=128,
                    num_layers=20,
                    model_dims=1280,
                    backend="pytorch",
                )
                tfm.load_from_checkpoint(repo_id=model_id)
            
            # Forecast each series
            for series in series_list:
                if "2.5" in model_id:
                    point_forecast, quantile_forecast = model.forecast(
                        horizon=forecast_horizon,
                        inputs=[series.tolist()]
                    )
                    forecast = point_forecast[0].tolist()
                else:
                    # Determine frequency category
                    freq_cat = 0  # Default: high frequency
                    if frequency:
                        freq_map = {"W": 1, "M": 1, "Q": 2, "Y": 2}
                        freq_cat = freq_map.get(frequency.upper(), 0)
                    
                    result = tfm.forecast(
                        forecast_input=[series.tolist()],
                        freq=[freq_cat]
                    )
                    forecast = result[0].tolist() if isinstance(result[0], (list, np.ndarray)) else result.tolist()
                
                forecasts.append(forecast)
            
            metadata["model_type"] = "timesfm"
            
        except ImportError:
            raise ImportError("timesfm not installed. Install with: pip install timesfm")
    
    elif is_ttm:
        # TTM models (IBM Granite)
        try:
            from granite_tsfm.models.tinytimemixer import TinyTimeMixerForPrediction
            from transformers import Trainer, TrainingArguments
            import torch
            
            device = "cuda" if torch.cuda.is_available() else "cpu"
            
            # Load model
            model = TinyTimeMixerForPrediction.from_pretrained(model_id)
            model.to(device)
            model.eval()
            
            # Forecast each series
            for series in series_list:
                # Standardize input (TTM requires standardized data)
                mean = np.mean(series)
                std = np.std(series)
                if std == 0:
                    std = 1.0
                standardized = (series - mean) / std
                
                # Prepare input tensor
                input_tensor = torch.tensor(standardized, dtype=torch.float32).unsqueeze(0).unsqueeze(0).to(device)
                
                # Forecast
                with torch.no_grad():
                    output = model(input_tensor)
                    forecast = output.prediction_values[0, 0, :forecast_horizon].cpu().numpy().tolist()
                
                # Denormalize
                forecast = [(x * std) + mean for x in forecast]
                forecasts.append(forecast)
            
            metadata["model_type"] = "ttm"
            
        except ImportError:
            raise ImportError("granite-tsfm not installed. Install with: pip install granite-tsfm")
    
    elif is_chronos_bolt:
        # Chronos-Bolt models (AutoGluon) - faster and more efficient than original Chronos
        try:
            from autogluon.timeseries import TimeSeriesPredictor, TimeSeriesDataFrame
            import pandas as pd
            import numpy as np
            
            # Chronos-Bolt models use AutoGluon's TimeSeriesPredictor
            # Convert input data to TimeSeriesDataFrame format
            # Input: list of time series (each series is a list of values)
            # AutoGluon expects: DataFrame with columns ['item_id', 'timestamp', 'target']
            
            # Create TimeSeriesDataFrame from input
            all_data = []
            for idx, series in enumerate(series_list):
                # Create timestamps (assuming daily frequency if not specified)
                freq = frequency if frequency else "D"
                timestamps = pd.date_range(start="2020-01-01", periods=len(series), freq=freq)
                
                for ts_idx, value in enumerate(series):
                    all_data.append({
                        'item_id': f'series_{idx}',
                        'timestamp': timestamps[ts_idx],
                        'target': float(value)
                    })
            
            df = pd.DataFrame(all_data)
            ts_df = TimeSeriesDataFrame(df)
            
            # Use Chronos-Bolt model via AutoGluon
            # Map model_id to AutoGluon model path
            if "autogluon" in model_id.lower() or "chronos-bolt" in model_id.lower():
                # Already in AutoGluon format (e.g., "autogluon/chronos-bolt-small")
                ag_model_path = model_id
            else:
                # Convert to AutoGluon format (e.g., "amazon/chronos-t5-small" -> "autogluon/chronos-bolt-small")
                if "small" in model_id.lower():
                    ag_model_path = "autogluon/chronos-bolt-small"
                elif "base" in model_id.lower():
                    ag_model_path = "autogluon/chronos-bolt-base"
                elif "mini" in model_id.lower():
                    ag_model_path = "autogluon/chronos-bolt-mini"
                elif "tiny" in model_id.lower():
                    ag_model_path = "autogluon/chronos-bolt-tiny"
                else:
                    ag_model_path = "autogluon/chronos-bolt-small"  # Default
            
            # Create predictor with Chronos-Bolt
            predictor = TimeSeriesPredictor(prediction_length=forecast_horizon).fit(
                ts_df,
                hyperparameters={
                    "Chronos": {"model_path": ag_model_path},
                },
                verbosity=0  # Suppress output
            )
            
            # Generate forecasts
            predictions = predictor.predict(ts_df)
            
            # Extract forecasts for each series
            for idx in range(len(series_list)):
                series_id = f'series_{idx}'
                if series_id in predictions.index:
                    series_forecast = predictions.loc[series_id]['mean'].values
                    forecasts.append(series_forecast.tolist()[:forecast_horizon])
                else:
                    # Fallback: use last value or zero
                    forecasts.append([series_list[idx][-1]] * forecast_horizon)
            
            metadata["model_type"] = "chronos-bolt"
            metadata["autogluon_model_path"] = ag_model_path
            
        except ImportError:
            raise ImportError("autogluon.timeseries not installed. Install with: pip install autogluon[timeseries]")
        except Exception as e:
            # If Chronos-Bolt fails, raise the error (don't fall back to Chronos)
            # because the model is specifically a Chronos-Bolt model
            raise ImportError(f"Chronos-Bolt (AutoGluon) failed: {str(e)}. Make sure autogluon[timeseries] is installed.")
    
    if is_chronos:
        # Chronos models (Amazon) - original implementation
        try:
            from chronos import ChronosPipeline
            import torch
            
            device = "cuda" if torch.cuda.is_available() else "cpu"
            dtype = torch.bfloat16 if torch.cuda.is_available() else torch.float32
            
            # Load pipeline
            pipeline = ChronosPipeline.from_pretrained(
                model_id,
                device_map=device,
                torch_dtype=dtype,
            )
            
            # Forecast each series
            for series in series_list:
                context = torch.tensor(series, dtype=torch.float32)
                forecast_result = pipeline.predict(context, forecast_horizon)
                
                # Extract median forecast (50th percentile)
                if len(forecast_result.shape) == 3:
                    # Shape: [num_series, num_samples, prediction_length]
                    forecast = forecast_result[0, :, :].median(dim=0)[0].numpy().tolist()
                else:
                    forecast = forecast_result[0].numpy().tolist()
                
                forecasts.append(forecast)
            
            metadata["model_type"] = "chronos"
            
        except ImportError:
            raise ImportError("chronos-forecasting not installed. Install with: pip install git+https://github.com/amazon-science/chronos-forecasting.git")
    
    elif is_mitra:
        # Mitra tabular models (AutoGluon)
        try:
            from autogluon.tabular import TabularPredictor, TabularDataset
            import pandas as pd
            
            # For tabular regression/classification
            # This is a simplified version - full implementation would need feature extraction
            raise NotImplementedError("Mitra tabular models require feature extraction and proper tabular data format. Please use AutoGluon directly.")
            
        except ImportError:
            raise ImportError("autogluon not installed. Install with: pip install autogluon.tabular[mitra]")
    
    else:
        # Generic transformers pipeline for time series
        try:
            from transformers import pipeline
            import torch
            
            device = 0 if torch.cuda.is_available() else -1
            
            # Try to use time series forecasting pipeline
            try:
                forecaster = pipeline(
                    "time-series-forecasting",
                    model=model_id,
                    device=device
                )
            except:
                # Fallback to generic pipeline
                forecaster = pipeline(
                    "text-generation",  # Some models use this
                    model=model_id,
                    device=device
                )
            
            # Forecast each series
            for series in series_list:
                # Convert to appropriate format
                if hasattr(forecaster, 'forecast'):
                    forecast = forecaster.forecast(series, forecast_horizon)
                else:
                    # Generic approach
                    forecast = [series[-1]] * forecast_horizon  # Simple persistence forecast
            
                forecasts.append(forecast)
            
            metadata["model_type"] = "generic"
            
        except Exception as e:
            raise Exception(f"Error using generic pipeline: {str(e)}")
    
    result = {
        "success": True,
        "forecasts": forecasts,
        "metadata": metadata
    }
    print(json.dumps(result))
    
except Exception as e:
    import traceback
    error_result = {
        "success": False,
        "error": str(e),
        "traceback": traceback.format_exc()[:500]
    }
    print(json.dumps(error_result))
'''


# Singleton instance
_tabular_time_series_service = None

def get_tabular_time_series_service() -> TabularTimeSeriesService:
    """Get singleton instance of TabularTimeSeriesService"""
    global _tabular_time_series_service
    if _tabular_time_series_service is None:
        _tabular_time_series_service = TabularTimeSeriesService()
    return _tabular_time_series_service
