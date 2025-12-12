/**
 * NLP Text Processing Nodes
 * Natural language processing operations
 */

const NLPNodes = {
    tokenize: {
        type: 'nlp_tokenize',
        name: 'Tokenize Text',
        category: 'nlp-text-processing',
        icon: 'bi-scissors',
        color: '#6f42c1',
        description: 'Tokenize text into words or sentences',
        defaults: {
            column: '',
            method: 'word',
            language: 'english'
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('method', 'Tokenization Method', 'select', {
                default: 'word',
                options: ['word', 'sentence'],
                help: 'Tokenize by words or sentences'
            }),
            BaseNode.createProperty('language', 'Language', 'text', {
                default: 'english',
                placeholder: 'english, spanish, french'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const method = data.method || 'word';
            const language = data.language || 'english';
            
            if (!column) return `# Tokenize: Column required`;
            
            let code = 'import nltk\n';
            code += `from nltk.tokenize import word_tokenize, sent_tokenize\n`;
            code += `nltk.download('punkt', quiet=True)\n`;
            code += `nltk.download('punkt_tab', quiet=True)\n`;
            code += `\n`;
            
            if (method === 'word') {
                code += `${inputVar}['${column}_tokens'] = ${inputVar}['${column}'].apply(lambda x: word_tokenize(str(x)) if pd.notna(x) else [])\n`;
            } else {
                code += `${inputVar}['${column}_sentences'] = ${inputVar}['${column}'].apply(lambda x: sent_tokenize(str(x)) if pd.notna(x) else [])\n`;
            }
            
            code += `print(f'Tokenized {column} using {method} tokenization')\n`;
            
            return code;
        }
    },

    removeStopwords: {
        type: 'nlp_remove_stopwords',
        name: 'Remove Stopwords',
        category: 'nlp-text-processing',
        icon: 'bi-x-circle',
        color: '#dc3545',
        description: 'Remove stopwords from text',
        defaults: {
            column: '',
            language: 'english'
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('language', 'Language', 'text', {
                default: 'english',
                placeholder: 'english, spanish, french'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const language = data.language || 'english';
            
            if (!column) return `# Remove Stopwords: Column required`;
            
            let code = 'import nltk\n';
            code += `from nltk.corpus import stopwords\n`;
            code += `nltk.download('stopwords', quiet=True)\n`;
            code += `\n`;
            code += `stop_words = set(stopwords.words('${language}'))\n`;
            code += `${inputVar}['${column}_no_stopwords'] = ${inputVar}['${column}'].apply(\n`;
            code += `    lambda x: ' '.join([word for word in str(x).split() if word.lower() not in stop_words]) if pd.notna(x) else ''\n`;
            code += `)\n`;
            code += `print(f'Removed stopwords from {column}')\n`;
            
            return code;
        }
    },

    lemmatize: {
        type: 'nlp_lemmatize',
        name: 'Lemmatize',
        category: 'nlp-text-processing',
        icon: 'bi-arrow-down',
        color: '#6f42c1',
        description: 'Lemmatize words (reduce to root form)',
        defaults: {
            column: '',
            language: 'english'
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('language', 'Language', 'text', {
                default: 'english'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const language = data.language || 'english';
            
            if (!column) return `# Lemmatize: Column required`;
            
            let code = 'import nltk\n';
            code += `from nltk.stem import WordNetLemmatizer\n`;
            code += `nltk.download('wordnet', quiet=True)\n`;
            code += `nltk.download('omw-1.4', quiet=True)\n`;
            code += `\n`;
            code += `lemmatizer = WordNetLemmatizer()\n`;
            code += `${inputVar}['${column}_lemmatized'] = ${inputVar}['${column}'].apply(\n`;
            code += `    lambda x: ' '.join([lemmatizer.lemmatize(word) for word in str(x).split()]) if pd.notna(x) else ''\n`;
            code += `)\n`;
            code += `print(f'Lemmatized {column}')\n`;
            
            return code;
        }
    },

    tfidfVectorizer: {
        type: 'nlp_tfidf',
        name: 'TF-IDF Vectorization',
        category: 'nlp-text-processing',
        icon: 'bi-list-ul',
        color: '#007bff',
        description: 'Convert text to TF-IDF vectors',
        defaults: {
            column: '',
            max_features: 1000,
            ngram_range: '1,1'
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('max_features', 'Max Features', 'number', {
                default: 1000,
                min: 10,
                max: 10000,
                help: 'Maximum number of features'
            }),
            BaseNode.createProperty('ngram_range', 'N-gram Range', 'text', {
                default: '1,1',
                placeholder: '1,1 or 1,2',
                help: 'Comma-separated min,max n-gram range'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const maxFeatures = data.max_features || 1000;
            const ngramRange = data.ngram_range || '1,1';
            
            if (!column) return `# TF-IDF: Column required`;
            
            const [minN, maxN] = ngramRange.split(',').map(n => parseInt(n.trim()));
            
            let code = 'from sklearn.feature_extraction.text import TfidfVectorizer\n';
            code += `vectorizer = TfidfVectorizer(max_features=${maxFeatures}, ngram_range=(${minN}, ${maxN}))\n`;
            code += `tfidf_matrix = vectorizer.fit_transform(${inputVar}['${column}'])\n`;
            code += `print(f'TF-IDF matrix shape: {tfidf_matrix.shape}')\n`;
            code += `print(f'Vocabulary size: {len(vectorizer.vocabulary_)}')\n`;
            
            return code;
        }
    },

    wordEmbeddings: {
        type: 'nlp_word_embeddings',
        name: 'Word Embeddings',
        category: 'nlp-text-processing',
        icon: 'bi-diagram-3',
        color: '#6f42c1',
        description: 'Generate word embeddings (requires gensim or transformers)',
        defaults: {
            column: '',
            method: 'word2vec',
            vector_size: 100
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('method', 'Method', 'select', {
                default: 'word2vec',
                options: ['word2vec', 'fasttext', 'glove'],
                help: 'Embedding method'
            }),
            BaseNode.createProperty('vector_size', 'Vector Size', 'number', {
                default: 100,
                min: 50,
                max: 500,
                help: 'Dimension of word vectors'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const method = data.method || 'word2vec';
            const vectorSize = data.vector_size || 100;
            
            if (!column) return `# Word Embeddings: Column required`;
            
            let code = '';
            if (method === 'word2vec') {
                code += 'from gensim.models import Word2Vec\n';
                code += `# Tokenize first if needed\n`;
                code += `sentences = ${inputVar}['${column}'].apply(lambda x: str(x).split() if pd.notna(x) else []).tolist()\n`;
                code += `model = Word2Vec(sentences, vector_size=${vectorSize}, window=5, min_count=1, workers=4)\n`;
                code += `print(f'Word2Vec model trained with {len(model.wv)} words')\n`;
            } else if (method === 'fasttext') {
                code += 'from gensim.models import FastText\n';
                code += `sentences = ${inputVar}['${column}'].apply(lambda x: str(x).split() if pd.notna(x) else []).tolist()\n`;
                code += `model = FastText(sentences, vector_size=${vectorSize}, window=5, min_count=1, workers=4)\n`;
                code += `print(f'FastText model trained')\n`;
            }
            
            return code;
        }
    }
};

// Register all NLP nodes
Object.values(NLPNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = NLPNodes;
}

