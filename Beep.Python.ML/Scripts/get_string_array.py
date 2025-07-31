# Get string array from session variable
import json

variable_name = '{variable_name}'

try:
    if variable_name in globals():
        variable_value = globals()[variable_name]
        if isinstance(variable_value, (list, tuple)):
            # Convert to list of strings
            result_json = json.dumps([str(item) for item in variable_value])
        elif isinstance(variable_value, str):
            # If it's already a string, try to parse as JSON first
            try:
                parsed = json.loads(variable_value)
                if isinstance(parsed, list):
                    result_json = variable_value
                else:
                    result_json = json.dumps([variable_value])
            except:
                result_json = json.dumps([variable_value])
        else:
            # Convert other types to string array
            result_json = json.dumps([str(variable_value)])
    else:
        result_json = '[]'
        
    # Store result for retrieval
    globals()['result_json'] = result_json
    
except Exception as e:
    print(f"Error getting string array from session: {str(e)}")
    result_json = '[]'
    globals()['result_json'] = result_json