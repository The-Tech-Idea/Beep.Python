# Time series augmentation techniques
import pandas as pd
import numpy as np

# Parameters
time_series_columns = {time_series_columns}
augmentation_type = '{augmentation_type}'
parameter = {parameter}

try:
    # Check if data exists
    if 'data' not in globals() and 'train_data' not in globals():
        print("Error: No data available for time series augmentation")
        time_series_augmentation_successful = False
        globals()['time_series_augmentation_successful'] = time_series_augmentation_successful
        exit()
    
    # Determine which data to process
    data_to_process = []
    if 'train_data' in globals():
        data_to_process.append(('train_data', train_data))
    if 'test_data' in globals():
        data_to_process.append(('test_data', test_data))
    if 'data' in globals() and len(data_to_process) == 0:
        data_to_process.append(('data', data))
    
    print(f"Applying {augmentation_type} augmentation to time series columns: {time_series_columns}")
    print(f"Augmentation parameter: {parameter}")
    
    augmentation_info = {
        'augmentation_type': augmentation_type,
        'parameter': parameter,
        'columns_processed': [],
        'original_shapes': {},
        'augmented_shapes': {}
    }
    
    for data_name, data_df in data_to_process:
        # Validate time series columns exist
        available_columns = data_df.columns.tolist()
        valid_ts_columns = [col for col in time_series_columns if col in available_columns]
        
        if not valid_ts_columns:
            print(f"Warning: None of the specified time series columns found in {data_name}")
            continue
        
        print(f"Processing {data_name} with columns: {valid_ts_columns}")
        augmentation_info['original_shapes'][data_name] = data_df.shape
        
        for col in valid_ts_columns:
            if augmentation_type == 'noise':
                # Add Gaussian noise
                noise_std = parameter
                original_values = data_df[col].copy()
                noise = np.random.normal(0, noise_std * np.std(original_values), len(original_values))
                data_df[f'{col}_noise'] = original_values + noise
                augmentation_info['columns_processed'].append(f'{col}_noise')
                
            elif augmentation_type == 'scaling':
                # Apply random scaling
                scale_factor = parameter
                scaling_factors = np.random.uniform(1 - scale_factor, 1 + scale_factor, len(data_df))
                data_df[f'{col}_scaled'] = data_df[col] * scaling_factors
                augmentation_info['columns_processed'].append(f'{col}_scaled')
                
            elif augmentation_type == 'jittering':
                # Time jittering (small random shifts)
                max_jitter = int(parameter)
                if max_jitter > 0:
                    jitter_values = np.random.randint(-max_jitter, max_jitter + 1, len(data_df))
                    # Create jittered version by shifting indices
                    jittered_series = data_df[col].copy()
                    for i, jitter in enumerate(jitter_values):
                        new_idx = max(0, min(len(data_df) - 1, i + jitter))
                        jittered_series.iloc[i] = data_df[col].iloc[new_idx]
                    data_df[f'{col}_jittered'] = jittered_series
                    augmentation_info['columns_processed'].append(f'{col}_jittered')
                
            elif augmentation_type == 'window_warping':
                # Window warping (time distortion)
                window_size = int(parameter)
                if window_size > 1:
                    warped_series = data_df[col].copy()
                    for i in range(0, len(data_df) - window_size, window_size):
                        window = warped_series[i:i + window_size]
                        # Random warping within window
                        warp_factor = np.random.uniform(0.8, 1.2)
                        if warp_factor != 1.0:
                            new_indices = np.linspace(0, len(window) - 1, int(len(window) * warp_factor))
                            warped_window = np.interp(new_indices, range(len(window)), window)
                            # Resample back to original size
                            final_window = np.interp(range(len(window)), 
                                                   np.linspace(0, len(window) - 1, len(warped_window)), 
                                                   warped_window)
                            warped_series[i:i + window_size] = final_window
                    
                    data_df[f'{col}_warped'] = warped_series
                    augmentation_info['columns_processed'].append(f'{col}_warped')
                
            elif augmentation_type == 'magnitude_warping':
                # Magnitude warping (amplitude distortion)
                warp_strength = parameter
                # Generate smooth random curve for warping
                smooth_curve = np.random.normal(1.0, warp_strength, len(data_df))
                # Apply smoothing
                from scipy.ndimage import gaussian_filter1d
                smooth_curve = gaussian_filter1d(smooth_curve, sigma=len(data_df) * 0.1)
                
                data_df[f'{col}_mag_warped'] = data_df[col] * smooth_curve
                augmentation_info['columns_processed'].append(f'{col}_mag_warped')
                
            elif augmentation_type == 'permutation':
                # Segment permutation
                n_segments = int(parameter)
                if n_segments > 1:
                    segment_size = len(data_df) // n_segments
                    permuted_series = data_df[col].copy()
                    
                    # Create segments and shuffle them
                    segments = []
                    for i in range(0, len(data_df), segment_size):
                        segments.append(permuted_series[i:i + segment_size])
                    
                    np.random.shuffle(segments)
                    
                    # Reassemble
                    permuted_data = pd.concat(segments, ignore_index=True)
                    if len(permuted_data) != len(data_df):
                        permuted_data = permuted_data[:len(data_df)]
                    
                    data_df[f'{col}_permuted'] = permuted_data.values
                    augmentation_info['columns_processed'].append(f'{col}_permuted')
                    
            else:
                print(f"Warning: Unknown augmentation type '{augmentation_type}'")
                continue
        
        augmentation_info['augmented_shapes'][data_name] = data_df.shape
        
        # Update global data
        globals()[data_name] = data_df
    
    # Store augmentation information
    globals()['augmentation_info'] = augmentation_info
    
    print(f"Time series augmentation completed successfully!")
    print(f"Augmentation type: {augmentation_type}")
    print(f"Columns processed: {len(augmentation_info['columns_processed'])}")
    print(f"New columns created: {augmentation_info['columns_processed']}")
    
    time_series_augmentation_successful = True
    
except Exception as e:
    print(f"Error during time series augmentation: {str(e)}")
    time_series_augmentation_successful = False

# Store the result
globals()['time_series_augmentation_successful'] = time_series_augmentation_successful