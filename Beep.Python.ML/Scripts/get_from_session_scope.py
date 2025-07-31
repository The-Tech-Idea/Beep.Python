# Get data from session scope with type conversion
import json

variable_name = '{variable_name}'

try:
    if variable_name in globals():
        variable_value = globals()[variable_name]
        
        if isinstance(variable_value, (list, dict, str, int, float, bool)):
            result_json = json.dumps(variable_value)
        else:
            # Convert other types to string representation
            result_json = json.dumps(str(variable_value))
    else:
        result_json = 'null'
        
    # Store result for retrieval
    globals()['result_json'] = result_json
    
except Exception as e:
    print(f"Error getting data from session scope: {str(e)}")
    result_json = 'null'
    globals()['result_json'] = result_json