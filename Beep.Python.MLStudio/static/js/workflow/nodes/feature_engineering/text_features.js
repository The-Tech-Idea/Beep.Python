/**
 * Text-Based Feature Engineering Nodes
 * Extract features from text columns
 */

const TextFeatureNodes = {
    extractTextStats: {
        type: 'fe_extract_text_stats',
        name: 'Extract Text Statistics',
        category: 'feature-engineering',
        icon: 'bi-text-paragraph',
        color: '#1976d2',
        description: 'Extract basic statistics from text (word count, char count, etc.)',
        defaults: {
            column: '',
            features: 'word_count,char_count,upper_count,digit_count'
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('features', 'Features to Extract', 'text', {
                default: 'word_count,char_count,upper_count,digit_count',
                placeholder: 'word_count,char_count,upper_count,digit_count,sentence_count',
                help: 'Comma-separated text features'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const features = data.features || 'word_count,char_count,upper_count,digit_count';
            
            if (!column) return `# Extract Text Stats: Column required`;
            
            const featureList = features.split(',').map(f => f.trim());
            let code = `# Text statistics for ${column}\n`;
            
            if (featureList.includes('word_count')) {
                code += `${inputVar}['${column}_word_count'] = ${inputVar}['${column}'].astype(str).apply(lambda x: len(x.split()))\n`;
            }
            if (featureList.includes('char_count')) {
                code += `${inputVar}['${column}_char_count'] = ${inputVar}['${column}'].astype(str).str.len()\n`;
            }
            if (featureList.includes('upper_count')) {
                code += `${inputVar}['${column}_upper_count'] = ${inputVar}['${column}'].astype(str).str.findall(r'[A-Z]').str.len()\n`;
            }
            if (featureList.includes('digit_count')) {
                code += `${inputVar}['${column}_digit_count'] = ${inputVar}['${column}'].astype(str).str.findall(r'\\d').str.len()\n`;
            }
            if (featureList.includes('sentence_count')) {
                code += `import re\n`;
                code += `${inputVar}['${column}_sentence_count'] = ${inputVar}['${column}'].astype(str).apply(lambda x: len(re.split(r'[.!?]+', x)))\n`;
            }
            if (featureList.includes('special_char_count')) {
                code += `${inputVar}['${column}_special_char_count'] = ${inputVar}['${column}'].astype(str).str.findall(r'[^A-Za-z0-9\\s]').str.len()\n`;
            }
            
            code += `print(f'Extracted {len(featureList)} text features from {column}')\n`;
            
            return code;
        }
    },

    extractNgrams: {
        type: 'fe_extract_ngrams',
        name: 'Extract N-grams',
        category: 'feature-engineering',
        icon: 'bi-list-ul',
        color: '#0277bd',
        description: 'Extract n-gram features from text',
        defaults: {
            column: '',
            n: 2,
            max_features: 100
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('n', 'N-gram Size', 'number', {
                default: 2,
                min: 1,
                max: 5,
                help: 'Size of n-grams (1=unigrams, 2=bigrams, etc.)'
            }),
            BaseNode.createProperty('max_features', 'Max Features', 'number', {
                default: 100,
                min: 10,
                max: 1000,
                help: 'Maximum number of n-gram features'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const n = data.n || 2;
            const maxFeatures = data.max_features || 100;
            
            if (!column) return `# Extract N-grams: Column required`;
            
            let code = 'from sklearn.feature_extraction.text import CountVectorizer\n';
            code += `vectorizer = CountVectorizer(ngram_range=(${n}, ${n}), max_features=${maxFeatures})\n`;
            code += `ngram_features = vectorizer.fit_transform(${inputVar}['${column}'].astype(str))\n`;
            code += `ngram_df = pd.DataFrame(ngram_features.toarray(), columns=vectorizer.get_feature_names_out())\n`;
            code += `${inputVar} = pd.concat([${inputVar}, ngram_df.add_prefix('${column}_ngram_')], axis=1)\n`;
            code += `print(f'Extracted {len(vectorizer.get_feature_names_out())} n-gram features')\n`;
            
            return code;
        }
    },

    extractTFIDF: {
        type: 'fe_extract_tfidf',
        name: 'Extract TF-IDF Features',
        category: 'feature-engineering',
        icon: 'bi-graph-up',
        color: '#e65100',
        description: 'Extract TF-IDF features from text',
        defaults: {
            column: '',
            max_features: 100,
            ngram_range: '1,1'
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('max_features', 'Max Features', 'number', {
                default: 100,
                min: 10,
                max: 1000
            }),
            BaseNode.createProperty('ngram_range', 'N-gram Range', 'text', {
                default: '1,1',
                placeholder: '1,2 for unigrams and bigrams',
                help: 'Comma-separated min,max n-gram range'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const maxFeatures = data.max_features || 100;
            const ngramRange = data.ngram_range || '1,1';
            
            if (!column) return `# Extract TF-IDF: Column required`;
            
            const [minN, maxN] = ngramRange.split(',').map(n => parseInt(n.trim()));
            
            let code = 'from sklearn.feature_extraction.text import TfidfVectorizer\n';
            code += `vectorizer = TfidfVectorizer(ngram_range=(${minN}, ${maxN}), max_features=${maxFeatures})\n`;
            code += `tfidf_features = vectorizer.fit_transform(${inputVar}['${column}'].astype(str))\n`;
            code += `tfidf_df = pd.DataFrame(tfidf_features.toarray(), columns=vectorizer.get_feature_names_out())\n`;
            code += `${inputVar} = pd.concat([${inputVar}, tfidf_df.add_prefix('${column}_tfidf_')], axis=1)\n`;
            code += `print(f'Extracted {len(vectorizer.get_feature_names_out())} TF-IDF features')\n`;
            
            return code;
        }
    },

    extractSentiment: {
        type: 'fe_extract_sentiment',
        name: 'Extract Sentiment',
        category: 'feature-engineering',
        icon: 'bi-emoji-smile',
        color: '#2e7d32',
        description: 'Extract sentiment scores from text (requires textblob or vaderSentiment)',
        defaults: {
            column: '',
            method: 'textblob'
        },
        properties: [
            BaseNode.createProperty('column', 'Text Column', 'text', {
                required: true,
                placeholder: 'text_column'
            }),
            BaseNode.createProperty('method', 'Sentiment Method', 'select', {
                default: 'textblob',
                options: ['textblob', 'vader'],
                help: 'Method for sentiment analysis'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const method = data.method || 'textblob';
            
            if (!column) return `# Extract Sentiment: Column required`;
            
            let code = '';
            if (method === 'textblob') {
                code += 'from textblob import TextBlob\n';
                code += `${inputVar}['${column}_sentiment_polarity'] = ${inputVar}['${column}'].astype(str).apply(lambda x: TextBlob(x).sentiment.polarity)\n`;
                code += `${inputVar}['${column}_sentiment_subjectivity'] = ${inputVar}['${column}'].astype(str).apply(lambda x: TextBlob(x).sentiment.subjectivity)\n`;
            } else {
                code += 'from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer\n';
                code += `analyzer = SentimentIntensityAnalyzer()\n`;
                code += `sentiment_scores = ${inputVar}['${column}'].astype(str).apply(lambda x: analyzer.polarity_scores(x))\n`;
                code += `${inputVar}['${column}_sentiment_compound'] = sentiment_scores.apply(lambda x: x['compound'])\n`;
                code += `${inputVar}['${column}_sentiment_pos'] = sentiment_scores.apply(lambda x: x['pos'])\n`;
                code += `${inputVar}['${column}_sentiment_neg'] = sentiment_scores.apply(lambda x: x['neg'])\n`;
                code += `${inputVar}['${column}_sentiment_neu'] = sentiment_scores.apply(lambda x: x['neu'])\n`;
            }
            code += `print(f'Extracted sentiment features from {column}')\n`;
            
            return code;
        }
    }
};

// Register all text feature nodes
Object.values(TextFeatureNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = TextFeatureNodes;
}

