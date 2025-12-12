/**
 * Hyperparameter Tuning Nodes
 * Model optimization and hyperparameter search
 */

const HyperparameterTuningNodes = {
    gridSearch: {
        type: 'sklearn_grid_search',
        name: 'Grid Search',
        category: 'hyperparameter-tuning',
        icon: 'bi-grid-3x3',
        color: '#7b1fa2',
        description: 'Exhaustive search over specified parameter values',
        defaults: {
            cv: 5,
            scoring: null,
            n_jobs: -1,
            verbose: 1
        },
        properties: [
            BaseNode.createProperty('cv', 'CV Folds', 'number', {
                default: 5,
                min: 2,
                max: 20
            }),
            BaseNode.createProperty('scoring', 'Scoring Metric', 'select', {
                default: null,
                options: [null, 'accuracy', 'precision', 'recall', 'f1', 'roc_auc', 'neg_mean_squared_error', 'r2'],
                help: 'Scoring metric for evaluation'
            }),
            BaseNode.createProperty('n_jobs', 'N Jobs', 'number', {
                default: -1,
                help: 'Number of parallel jobs (-1 for all cores)'
            }),
            BaseNode.createProperty('verbose', 'Verbose', 'number', {
                default: 1,
                min: 0,
                max: 3,
                help: 'Verbosity level (0=silent, 3=most verbose)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X_train';
            const cv = data.cv || 5;
            const scoring = data.scoring || null;
            const nJobs = data.n_jobs !== undefined ? data.n_jobs : -1;
            const verbose = data.verbose !== undefined ? data.verbose : 1;
            
            let code = 'from sklearn.model_selection import GridSearchCV\n';
            code += `# Define parameter grid (example)\n`;
            code += `param_grid = {\n`;
            code += `    'n_estimators': [50, 100, 200],\n`;
            code += `    'max_depth': [None, 10, 20],\n`;
            code += `    'min_samples_split': [2, 5, 10]\n`;
            code += `}\n`;
            code += `# Example estimator: from sklearn.ensemble import RandomForestClassifier\n`;
            code += `# estimator = RandomForestClassifier()\n`;
            
            const params = [`cv=${cv}`, `n_jobs=${nJobs}`, `verbose=${verbose}`];
            if (scoring) params.push(`scoring='${scoring}'`);
            
            code += `grid_search = GridSearchCV(estimator, param_grid, ${params.join(', ')})\n`;
            code += `grid_search.fit(${inputVar}, y_train)\n`;
            code += `print(f'Best parameters: {grid_search.best_params_}')\n`;
            code += `print(f'Best score: {grid_search.best_score_}')\n`;
            code += `best_model = grid_search.best_estimator_\n`;
            
            return code;
        }
    },

    randomizedSearch: {
        type: 'sklearn_randomized_search',
        name: 'Randomized Search',
        category: 'hyperparameter-tuning',
        icon: 'bi-shuffle',
        color: '#c2185b',
        description: 'Randomized search on hyper parameters',
        defaults: {
            n_iter: 10,
            cv: 5,
            random_state: 42,
            n_jobs: -1
        },
        properties: [
            BaseNode.createProperty('n_iter', 'N Iterations', 'number', {
                default: 10,
                min: 1,
                max: 100,
                help: 'Number of parameter settings sampled'
            }),
            BaseNode.createProperty('cv', 'CV Folds', 'number', {
                default: 5,
                min: 2,
                max: 20
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            }),
            BaseNode.createProperty('n_jobs', 'N Jobs', 'number', {
                default: -1
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X_train';
            const nIter = data.n_iter || 10;
            const cv = data.cv || 5;
            const randomState = data.random_state || 42;
            const nJobs = data.n_jobs !== undefined ? data.n_jobs : -1;
            
            let code = 'from sklearn.model_selection import RandomizedSearchCV\n';
            code += 'from scipy.stats import randint, uniform\n';
            code += `# Define parameter distribution (example)\n`;
            code += `param_dist = {\n`;
            code += `    'n_estimators': randint(50, 200),\n`;
            code += `    'max_depth': [None, 10, 20, 30],\n`;
            code += `    'min_samples_split': randint(2, 10)\n`;
            code += `}\n`;
            code += `# Example estimator: from sklearn.ensemble import RandomForestClassifier\n`;
            code += `# estimator = RandomForestClassifier()\n`;
            
            code += `random_search = RandomizedSearchCV(estimator, param_dist, n_iter=${nIter}, cv=${cv}, random_state=${randomState}, n_jobs=${nJobs})\n`;
            code += `random_search.fit(${inputVar}, y_train)\n`;
            code += `print(f'Best parameters: {random_search.best_params_}')\n`;
            code += `print(f'Best score: {random_search.best_score_}')\n`;
            code += `best_model = random_search.best_estimator_\n`;
            
            return code;
        }
    },

    bayesianOptimization: {
        type: 'sklearn_bayesian_optimization',
        name: 'Bayesian Optimization',
        category: 'hyperparameter-tuning',
        icon: 'bi-graph-up-arrow',
        color: '#ff6b6b',
        description: 'Bayesian optimization for hyperparameters (requires scikit-optimize)',
        defaults: {
            n_calls: 10,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_calls', 'N Calls', 'number', {
                default: 10,
                min: 1,
                max: 100,
                help: 'Number of iterations'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X_train';
            const nCalls = data.n_calls || 10;
            const randomState = data.random_state || 42;
            
            let code = 'from skopt import gp_minimize\n';
            code += 'from skopt.space import Real, Integer\n';
            code += `# Define search space (example)\n`;
            code += `space = [\n`;
            code += `    Integer(50, 200, name='n_estimators'),\n`;
            code += `    Integer(1, 20, name='max_depth'),\n`;
            code += `    Real(0.01, 1.0, name='learning_rate')\n`;
            code += `]\n`;
            code += `# Define objective function\n`;
            code += `def objective(params):\n`;
            code += `    # model = create_model(params)\n`;
            code += `    # score = cross_val_score(model, X_train, y_train, cv=5).mean()\n`;
            code += `    # return -score  # Minimize negative score\n`;
            code += `    pass\n`;
            code += `result = gp_minimize(objective, space, n_calls=${nCalls}, random_state=${randomState})\n`;
            code += `print(f'Best parameters: {result.x}')\n`;
            code += `print(f'Best score: {-result.fun}')\n`;
            
            return code;
        }
    }
};

// Register all hyperparameter tuning nodes
Object.values(HyperparameterTuningNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = HyperparameterTuningNodes;
}

