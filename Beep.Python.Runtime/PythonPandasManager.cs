using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Container.Services;
namespace Beep.Python.RuntimeEngine
{
    public class PythonPandasManager: PythonBaseViewModel
    {
       
        public PythonPandasManager(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager) : base(beepservice, pythonRuntimeManager)
        {
            //  pythonRuntimeManager = pythonRuntimeManager;
            InitializePythonEnvironment();
        }
        #region "Pandas DataFrame"
        public void CreateDataFrame(string dataFrameName, dynamic data)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = pd.DataFrame({data})";
                PersistentScope.Exec(script);
            }
        }
        public void AddColumn(string dataFrameName, string columnName, dynamic columnData)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = {columnData}";
                PersistentScope.Exec(script);
            }
        }
        public string GetDataFrameAsJson(string dataFrameName)
        {
            using (Py.GIL())
            {
                dynamic pandasDataFrame = PersistentScope.Get(dataFrameName);
                return pandasDataFrame.to_json();
            }
        }
        public void ReadCsv(string dataFrameName, string filePath)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = pd.read_csv('{filePath}')";
                PersistentScope.Exec(script);
            }
        }
        public void ReadExcel(string dataFrameName, string filePath)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = pd.read_excel('{filePath}')";
                PersistentScope.Exec(script);
            }
        }
        public void SelectColumns(string dataFrameName, string newFrameName, string[] columns)
        {
            var columnsStr = string.Join(", ", columns.Select(c => $"'{c}'"));
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}[[{columnsStr}]]";
                PersistentScope.Exec(script);
            }
        }
        public void FilterRows(string dataFrameName, string newFrameName, string condition)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}[{dataFrameName}[{condition}]]";
                PersistentScope.Exec(script);
            }
        }
        public void GroupBy(string dataFrameName, string newFrameName, string groupByColumn, string aggFunc)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.groupby('{groupByColumn}').{aggFunc}()";
                PersistentScope.Exec(script);
            }
        }
        public void MergeDataFrames(string leftFrameName, string rightFrameName, string newFrameName, string onColumn)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = pd.merge({leftFrameName}, {rightFrameName}, on='{onColumn}')";
                PersistentScope.Exec(script);
            }
        }
        public void ToCsv(string dataFrameName, string filePath)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.to_csv('{filePath}')";
                PersistentScope.Exec(script);
            }
        }
        public void ToExcel(string dataFrameName, string filePath)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.to_excel('{filePath}')";
                PersistentScope.Exec(script);
            }
        }
        public void DropNA(string dataFrameName, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.dropna()";
                PersistentScope.Exec(script);
            }
        }
        public void FillNA(string dataFrameName, string newFrameName, dynamic value)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.fillna({value})";
                PersistentScope.Exec(script);
            }
        }
        public string Describe(string dataFrameName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}.describe()");
                return result.ToString();
            }
        }
        public string Correlation(string dataFrameName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}.corr()");
                return result.ToString();
            }
        }
        public void PivotTable(string dataFrameName, string newFrameName, string index, string columns, string values)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.pivot_table(index='{index}', columns='{columns}', values='{values}')";
                PersistentScope.Exec(script);
            }
        }
        public void Resample(string dataFrameName, string newFrameName, string rule, string aggFunc)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.resample('{rule}').{aggFunc}()";
                PersistentScope.Exec(script);
            }
        }
        public void ApplyFunction(string dataFrameName, string columnName, string func)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = {dataFrameName}['{columnName}'].apply({func})";
                PersistentScope.Exec(script);
            }
        }
        public void SortByColumn(string dataFrameName, string columnName, bool ascending = true)
        {
            using (Py.GIL())
            {
                string script = $@"{dataFrameName}.sort_values(by=''{columnName}'', ascending={ascending.ToString().ToLower()})";
                PersistentScope.Exec(script);
            }
        }

        public void Rank(string dataFrameName, string newFrameName, string method = "average")
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.rank(method='{method}')";
                PersistentScope.Exec(script);
            }
        }
        public string UniqueValues(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].unique()");
                return result.ToString();
            }
        }
        public string ValueCounts(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].value_counts()");
                return result.ToString();
            }
        }
        public void ConcatDataFrames(string[] dataFrameNames, string newFrameName)
        {
            var frames = string.Join(", ", dataFrameNames);
            using (Py.GIL())
            {
                string script = $"{newFrameName} = pd.concat([{frames}])";
                PersistentScope.Exec(script);
            }
        }
        public void AppendToDataFrame(string dataFrameName, string otherDataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = {dataFrameName}.append({otherDataFrameName})";
                PersistentScope.Exec(script);
            }
        }
        public void DropDuplicates(string dataFrameName, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.drop_duplicates()";
                PersistentScope.Exec(script);
            }
        }
        public void RenameColumns(string dataFrameName, Dictionary<string, string> newColumnNames)
        {
            var renameMapping = string.Join(", ", newColumnNames.Select(kvp => $"'{kvp.Key}': '{kvp.Value}'"));
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.rename(columns={{ {renameMapping} }})";
                PersistentScope.Exec(script);
            }
        }
        public void DropColumn(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.drop(columns=['{columnName}'])";
                PersistentScope.Exec(script);
            }
        }
        public void ReorderColumns(string dataFrameName, string[] newOrder)
        {
            var columns = string.Join(", ", newOrder.Select(c => $"\"{c}\""));
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = {dataFrameName}[[{columns}]]";
                PersistentScope.Exec(script);
            }
        }
        public void NormalizeColumn(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = (({dataFrameName}['{columnName}'] - {dataFrameName}['{columnName}'].min()) / ({dataFrameName}['{columnName}'].max() - {dataFrameName}['{columnName}'].min()))";
                PersistentScope.Exec(script);
            }
        }
        public void ConvertDataType(string dataFrameName, string columnName, string newType)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = {dataFrameName}['{columnName}'].astype('{newType}')";
                PersistentScope.Exec(script);
            }
        }
        public void ToJson(string dataFrameName, string filePath)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.to_json('{filePath}')";
                PersistentScope.Exec(script);
            }
        }
        public void ReadJson(string dataFrameName, string filePath)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = pd.read_json('{filePath}')";
                PersistentScope.Exec(script);
            }
        }
        public void RollingWindow(string dataFrameName, string newFrameName, int windowSize, string operation)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.rolling(window={windowSize}).{operation}()";
                PersistentScope.Exec(script);
            }
        }
        public void CrossTab(string dataFrameName, string newFrameName, string index, string columns)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = pd.crosstab({dataFrameName}['{index}'], {dataFrameName}['{columns}'])";
                PersistentScope.Exec(script);
            }
        }
        public void StringOperation(string dataFrameName, string columnName, string operation)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = {dataFrameName}['{columnName}'].str.{operation}";
                PersistentScope.Exec(script);
            }
        }
        public void ConvertToDateTime(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = pd.to_datetime({dataFrameName}['{columnName}'])";
                PersistentScope.Exec(script);
            }
        }
        public void SetPandasOption(string optionName, string value)
        {
            using (Py.GIL())
            {
                string script = $"pd.set_option('{optionName}', {value})";
                PersistentScope.Exec(script);
            }
        }
        public void CreateMultiIndexDataFrame(string dataFrameName, string[] indexColumns)
        {
            var columns = string.Join(", ", indexColumns.Select(c => $"\"{c}\""));
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.set_index([{columns}])";
                PersistentScope.Exec(script);
            }
        }
        public void SampleDataFrame(string dataFrameName, string newFrameName, double fraction)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.sample(frac={fraction})";
                PersistentScope.Exec(script);
            }
        }
        public void QueryDataFrame(string dataFrameName, string newFrameName, string query)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.query(\"{query}\")";
                PersistentScope.Exec(script);
            }
        }
        public void ReadSql(string dataFrameName, string sql, string connectionString)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = pd.read_sql('{sql}', '{connectionString}')";
                PersistentScope.Exec(script);
            }
        }
        public void JsonNormalize(string dataFrameName, string jsonData)
        {
            using (Py.GIL())
            {
                string script = $"import json\n{dataFrameName} = pd.json_normalize(json.loads('{jsonData}'))";
                PersistentScope.Exec(script);
            }
        }
        public void TextAnalysis(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"import nltk\nnltk.download('punkt')\n{dataFrameName}['{columnName}_word_count'] = {dataFrameName}['{columnName}'].apply(lambda X: len(nltk.word_tokenize(X)))";
                PersistentScope.Exec(script);
            }
        }
        public void AdvancedFilter(string dataFrameName, string newFrameName, string condition)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.loc[{dataFrameName}.eval({condition})]";
                PersistentScope.Exec(script);
            }
        }
        public void OptimizeMemory(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = {dataFrameName}.convert_dtypes()";
                PersistentScope.Exec(script);
            }
        }
        public void ConvertToCategorical(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = {dataFrameName}['{columnName}'].astype('Category')";
                PersistentScope.Exec(script);
            }
        }
        public string PlotDataFrame(string dataFrameName, string plotType, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"import matplotlib.pyplot as plt\n{dataFrameName}.plot(kind='{plotType}', Y='{columnName}')\nplt.savefig('plot.png')";
                PersistentScope.Exec(script);
                return "plot.png";
            }
        }
        public void StringRegexOperation(string dataFrameName, string columnName, string regexPattern, string method)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = {dataFrameName}['{columnName}'].str.{method}(r'{regexPattern}')";
                PersistentScope.Exec(script);
            }
        }
        public void ApplyCustomFunction(string dataFrameName, string customFunction)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = {dataFrameName}.apply({customFunction})";
                PersistentScope.Exec(script);
            }
        }
        public void ReadFromWeb(string dataFrameName, string url)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = pd.read_json('{url}')";
                PersistentScope.Exec(script);
            }
        }
        public void ResetIndex(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = {dataFrameName}.reset_index(drop=True)";
                PersistentScope.Exec(script);
            }
        }
        public string StyleDataFrame(string dataFrameName)
        {
            using (Py.GIL())
            {
                dynamic styled = PersistentScope.Eval($"{dataFrameName}.style");
                return styled.render();
            }
        }
        public string IsNull(string dataFrameName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}.isnull()");
                return result.ToString();
            }
        }
        public void HandleComplexDataTypes(string dataFrameName, string columnName, string operation)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = {dataFrameName}['{columnName}'].apply(lambda X: X.{operation})";
                PersistentScope.Exec(script);
            }
        }
        public void FlattenDataFrame(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = {dataFrameName}.reset_index()";
                PersistentScope.Exec(script);
            }
        }
        public void AdvancedMerge(string[] dataFrameNames, string newFrameName, string joinType, string onColumn)
        {
            var frames = string.Join(", ", dataFrameNames);
            using (Py.GIL())
            {
                string script = $"{newFrameName} = pd.concat([{frames}], join='{joinType}', on='{onColumn}')";
                PersistentScope.Exec(script);
            }
        }
        public void ProcessInChunks(string dataFrameName, int chunkSize, string processFunction)
        {
            using (Py.GIL())
            {
                string script = $"for chunk in np.array_split({dataFrameName}, {chunkSize}):\n    {processFunction}(chunk)";
                PersistentScope.Exec(script);
            }
        }
        public void EncodeCategorical(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"from sklearn.preprocessing import LabelEncoder\nencoder = LabelEncoder()\n{dataFrameName}['{columnName}'] = encoder.fit_transform({dataFrameName}['{columnName}'])";
                PersistentScope.Exec(script);
            }
        }
        public void DataQualityCheck(string dataFrameName, string checkExpression)
        {
            using (Py.GIL())
            {
                string script = $"assert {dataFrameName}.eval({checkExpression})";
                PersistentScope.Exec(script);
            }
        }
        public string CreateInteractivePlot(string dataFrameName, string plotType, string[] columns)
        {
            using (Py.GIL())
            {
                string cols = string.Join(", ", columns.Select(c => $"\"{c}\""));
                string script = $"import plotly.express as px\nfig = px.{plotType}({dataFrameName}, {cols})\nfig.write_html('plot.html')";
                PersistentScope.Exec(script);
                return "plot.html";
            }
        }
        public void DetectOutliers(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"from scipy import stats\n{dataFrameName} = {dataFrameName}[(np.abs(stats.zscore({dataFrameName}['{columnName}'])) < 3)]";
                PersistentScope.Exec(script);
            }
        }
        public void FillNA(string dataFrameName, string value)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName} = {dataFrameName}.fillna({value})";
                PersistentScope.Exec(script);
            }
        }
        public void ReindexDataFrame(string dataFrameName, string newFrameName, string[] newIndices)
        {
            var indices = string.Join(", ", newIndices.Select(i => $"\"{i}\""));
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.reindex([{indices}])";
                PersistentScope.Exec(script);
            }
        }
        public void AddPrefixSuffix(string dataFrameName, string newFrameName, string prefix = "", string suffix = "")
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.add_prefix('{prefix}').add_suffix('{suffix}')";
                PersistentScope.Exec(script);
            }
        }

        public void CumulativeSum(string dataFrameName, string columnName, string newColumnName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{newColumnName}'] = {dataFrameName}['{columnName}'].cumsum()";
                PersistentScope.Exec(script);
            }
        }

        public string CalculateMean(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].mean()");
                return result.ToString();
            }
        }

        public void RollingAverage(string dataFrameName, string columnName, string newColumnName, int windowSize)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{newColumnName}'] = {dataFrameName}['{columnName}'].rolling(window={windowSize}).mean()";
                PersistentScope.Exec(script);
            }
        }

        public string MaxValue(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].max()");
                return result.ToString();
            }
        }
        public string MinValue(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].min()");
                return result.ToString();
            }
        }
        public string StandardDeviation(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].std()");
                return result.ToString();
            }
        }
        public string Variance(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].var()");
                return result.ToString();
            }
        }
        public string CalculateMedian(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].median()");
                return result.ToString();
            }
        }
        public string CalculateMode(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].mode()");
                return result.ToString();
            }
        }
        public string CalculateSum(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].sum()");
                return result.ToString();
            }
        }
        public string CountNonNull(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].count()");
                return result.ToString();
            }
        }
        public string DescribeDataFrame(string dataFrameName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}.describe()");
                return result.ToString();
            }
        }
        public string CalculateSkewness(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"from scipy.stats import skew\nskewness = skew({dataFrameName}['{columnName}'])");
                return result.ToString();
            }
        }
        public string CalculateKurtosis(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"from scipy.stats import kurtosis\nkurt = kurtosis({dataFrameName}['{columnName}'])");
                return result.ToString();
            }
        }
        public string CalculateQuantiles(string dataFrameName, string columnName, double[] quantiles)
        {
            using (Py.GIL())
            {
                string quantileStr = string.Join(", ", quantiles.Select(q => q.ToString()));
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].quantile([{quantileStr}])");
                return result.ToString();
            }
        }
        public string CalculateCovariance(string dataFrameName, string column1, string column2)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{column1}'].cov({dataFrameName}['{column2}'])");
                return result.ToString();
            }
        }
        public string CalculateCorrelation(string dataFrameName, string column1, string column2, string method = "pearson")
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{column1}'].corr({dataFrameName}['{column2}'], method='{method}')");
                return result.ToString();
            }
        }
        public string FrequencyDistribution(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"{dataFrameName}['{columnName}'].value_counts()");
                return result.ToString();
            }
        }
        public void FilterData(string dataFrameName, string condition, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}[{condition}]";
                PersistentScope.Exec(script);
            }
        }
        public void SampleData(string dataFrameName, int sampleSize, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.sample(n={sampleSize})";
                PersistentScope.Exec(script);
            }
        }
        public void ConcatenateDataFrames(string[] dataFrameNames, string newFrameName, string axis)
        {
            var frames = string.Join(", ", dataFrameNames);
            using (Py.GIL())
            {
                string script = $"{newFrameName} = pd.concat([{frames}], axis={axis})";
                PersistentScope.Exec(script);
            }
        }
        public void TransformData(string dataFrameName, string columnName, string newColumnName, string transformationFunction)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{newColumnName}'] = {dataFrameName}['{columnName}'].apply({transformationFunction})";
                PersistentScope.Exec(script);
            }
        }
        public void AggregateData(string dataFrameName, string groupByColumn, string aggregationFunction, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.groupby('{groupByColumn}').{aggregationFunction}()";
                PersistentScope.Exec(script);
            }
        }
        public void PivotData(string dataFrameName, string indexColumn, string columnsColumn, string valuesColumn, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.pivot(index='{indexColumn}', columns='{columnsColumn}', values='{valuesColumn}')";
                PersistentScope.Exec(script);
            }
        }
        public void MergeDataFrames(string dataFrame1Name, string dataFrame2Name, string onKey, string how, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = pd.merge({dataFrame1Name}, {dataFrame2Name}, on='{onKey}', how='{how}')";
                PersistentScope.Exec(script);
            }
        }
        public void MeltData(string dataFrameName, string idVars, string valueVars, string varName, string valueName, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = pd.melt({dataFrameName}, id_vars=[{idVars}], value_vars=[{valueVars}], var_name='{varName}', value_name='{valueName}')";
                PersistentScope.Exec(script);
            }
        }
        public void SortData(string dataFrameName, string[] columnsToSort, bool[] ascendingOrder, string newFrameName)
        {
            using (Py.GIL())
            {
                string sortParams = string.Join(", ", columnsToSort.Select((col, idx) => $"('{col}', {ascendingOrder[idx].ToString().ToLower()})"));
                string script = $"{newFrameName} = {dataFrameName}.sort_values(by=[{sortParams}])";
                PersistentScope.Exec(script);
            }
        }
        public void PivotTable(string dataFrameName, string indexColumn, string columnsColumn, string valuesColumn, string aggregationFunction, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.pivot_table(index='{indexColumn}', columns='{columnsColumn}', values='{valuesColumn}', aggfunc='{aggregationFunction}')";
                PersistentScope.Exec(script);
            }
        }
        public void BinData(string dataFrameName, string columnName, int numBins, string newColumnName)
        {
            using (Py.GIL())
            {
                string script = $"{newColumnName} = pd.cut({dataFrameName}['{columnName}'], bins={numBins})";
                PersistentScope.Exec(script);
            }
        }
        public void RankData(string dataFrameName, string[] columnsToRank, string newFrameName)
        {
            using (Py.GIL())
            {
                string rankColumns = string.Join(", ", columnsToRank.Select(col => $"'{col}'"));
                string script = $"{newFrameName} = {dataFrameName}[{rankColumns}].rank()";
                PersistentScope.Exec(script);
            }
        }
        public void ApplymapTransformation(string dataFrameName, string transformationFunction, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.applymap({transformationFunction})";
                PersistentScope.Exec(script);
            }
        }
        public void StringOperation(string dataFrameName, string columnName, string operation, string newColumnName)
        {
            using (Py.GIL())
            {
                string script = $"{newColumnName} = {dataFrameName}['{columnName}'].str.{operation}";
                PersistentScope.Exec(script);
            }
        }
        public void DatetimeOperation(string dataFrameName, string columnName, string operation, string newColumnName)
        {
            using (Py.GIL())
            {
                string script = $"{newColumnName} = {dataFrameName}['{columnName}'].dt.{operation}";
                PersistentScope.Exec(script);
            }
        }
        public void GroupByAndAggregate(string dataFrameName, string groupByColumn, string[] aggregationFunctions, string newFrameName)
        {
            using (Py.GIL())
            {
                string funcs = string.Join(", ", aggregationFunctions.Select(func => $"'{func}'"));
                string script = $"{newFrameName} = {dataFrameName}.groupby('{groupByColumn}').agg({{{funcs}}})";
                PersistentScope.Exec(script);
            }
        }
        public void SampleWithReplacement(string dataFrameName, int sampleSize, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.sample(n={sampleSize}, replace=True)";
                PersistentScope.Exec(script);
            }
        }
        public void JoinDataFrames(string dataFrame1Name, string dataFrame2Name, string onKey, string how, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrame1Name}.join({dataFrame2Name}, on='{onKey}', how='{how}')";
                PersistentScope.Exec(script);
            }
        }
        public string DataValidation(string dataFrameName)
        {
            using (Py.GIL())
            {
                dynamic result = PersistentScope.Eval($"data_validation_result = {{");
                result += PersistentScope.Eval($"{dataFrameName}.isnull().sum(): 'Missing Values',");
                result += PersistentScope.Eval($"{dataFrameName}.duplicated().sum(): 'Duplicate Rows',");
                result += PersistentScope.Eval($"}}");
                return result.ToString();
            }
        }
        public void ExportDataFrame(string dataFrameName, string filePath, string format)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.to_{format}('{filePath}')";
                PersistentScope.Exec(script);
            }
        }
        public void FillMissingValues(string dataFrameName, string method)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.fillna(method='{method}', inplace=True)";
                PersistentScope.Exec(script);
            }
        }
        public void StandardizeData(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}['{columnName}'] = (dataFrameName['{columnName}'] - dataFrameName['{columnName}'].mean()) / dataFrameName['{columnName}'].std()";
                PersistentScope.Exec(script);
            }
        }
        public void CrossTab(string index, string columns)
        {
            using (Py.GIL())
            {
                string script = $"pd.crosstab({index}, {columns})";
                RunPythonScript(script, null);
            }
        }
        public void Cut(string x, int bins)
        {
            using (Py.GIL())
            {
                string script = $"pd.cut({x}, {bins})";
                RunPythonScript(script, null);
            }
        }
        public void QCut(string x, int q)
        {
            using (Py.GIL())
            {
                string script = $"pd.qcut({x}, {q})";
                RunPythonScript(script, null);
            }
        }
        public void MergeOrdered(string left, string right, string on = null)
        {
            using (Py.GIL())
            {
                string script = $"pd.merge_ordered({left}, {right}, on='{on}')";
                RunPythonScript(script, null);
            }
        }
        public void MergeAsof(string left, string right, string on = null)
        {
            using (Py.GIL())
            {
                string script = $"pd.merge_asof({left}, {right}, on='{on}')";
                RunPythonScript(script, null);
            }
        }
        public void ConcatDataFrames(string[] dataFrames, int axis = 0)
        {
            using (Py.GIL())
            {
                string script = $"pd.concat({String.Join(", ", dataFrames)}, axis={axis})";
                RunPythonScript(script, null);
            }
        }
        public void GetDummies(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"pd.get_dummies({dataFrameName})";
                RunPythonScript(script, null);
            }
        }
        public void FromDummies(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"pd.from_dummies({dataFrameName})";
                RunPythonScript(script, null);
            }
        }
        public void Factorize(string values)
        {
            using (Py.GIL())
            {
                string script = $"pd.factorize({values})";
                RunPythonScript(script, null);
            }
        }
        public void UniqueValues(string values)
        {
            using (Py.GIL())
            {
                string script = $"pd.unique({values})";
                RunPythonScript(script, null);
            }
        }
        public void LReshape(string dataFrameName, Dictionary<string, string[]> groups)
        {
            using (Py.GIL())
            {
                string script = $"pd.lreshape({dataFrameName}, {groups})";
                RunPythonScript(script, null);
            }
        }
        public void WideToLong(string dataFrameName, string[] stubnames, string i, string j)
        {
            using (Py.GIL())
            {
                string script = $"pd.wide_to_long({dataFrameName}, stubnames={String.Join(", ", stubnames)}, i='{i}', j='{j}')";
                RunPythonScript(script, null);
            }
        }
        public void DetectMissingValues(string objName)
        {
            using (Py.GIL())
            {
                string script = $"pd.isna({objName})"; // or pd.isnull(objName)
                RunPythonScript(script, null);
            }
        }
        public void DetectNonMissingValues(string objName)
        {
            using (Py.GIL())
            {
                string script = $"pd.notna({objName})"; // or pd.notnull(objName)
                RunPythonScript(script, null);
            }
        }
        public void ConvertToNumeric(string arg, string errors = "raise")
        {
            using (Py.GIL())
            {
                string script = $"pd.to_numeric({arg}, errors='{errors}')";
                RunPythonScript(script, null);
            }
        }
        public void ConvertToDatetime(string arg, string errors = "raise")
        {
            using (Py.GIL())
            {
                string script = $"pd.to_datetime({arg}, errors='{errors}')";
                RunPythonScript(script, null);
            }
        }
        public void ConvertToTimedelta(string arg, string unit = "ns")
        {
            using (Py.GIL())
            {
                string script = $"pd.to_timedelta({arg}, unit='{unit}')";
                RunPythonScript(script, null);
            }
        }
        public void CreateDateRange(string start, string end, int periods, string freq = "D")
        {
            using (Py.GIL())
            {
                string script = $"pd.date_range(start='{start}', end='{end}', periods={periods}, freq='{freq}')";
                RunPythonScript(script, null);
            }
        }
        public void CreateBusinessDateRange(string start, string end, int periods, string freq = "B")
        {
            using (Py.GIL())
            {
                string script = $"pd.bdate_range(start='{start}', end='{end}', periods={periods}, freq='{freq}')";
                RunPythonScript(script, null);
            }
        }
        public void CreatePeriodRange(string start, string end, int periods, string freq = "D")
        {
            using (Py.GIL())
            {
                string script = $"pd.period_range(start='{start}', end='{end}', periods={periods}, freq='{freq}')";
                RunPythonScript(script, null);
            }
        }
        public void CreateTimedeltaRange(string start, string end, int periods, string freq = "D")
        {
            using (Py.GIL())
            {
                string script = $"pd.timedelta_range(start='{start}', end='{end}', periods={periods}, freq='{freq}')";
                RunPythonScript(script, null);
            }
        }
        public void InferFrequency(string indexName)
        {
            using (Py.GIL())
            {
                string script = $"pd.infer_freq({indexName})";
                RunPythonScript(script, null);
            }
        }
        public void CreateIntervalRange(string start, string end, int periods, string freq = "D")
        {
            using (Py.GIL())
            {
                string script = $"pd.interval_range(start='{start}', end='{end}', periods={periods}, freq='{freq}')";
                RunPythonScript(script, null);
            }
        }
        public void EvaluateExpression(string expr)
        {
            using (Py.GIL())
            {
                string script = $"pd.eval('{expr}')";
                RunPythonScript(script, null);
            }
        }
        public void GuessDatetimeFormat(string dtStr)
        {
            using (Py.GIL())
            {
                string script = $"pd.tseries.api.guess_datetime_format('{dtStr}')";
                RunPythonScript(script, null);
            }
        }
      

public dynamic CreateSeries(object data, IList<string> index = null, string dtype = null, string name = null)
    {
        using (Py.GIL())
        {
            dynamic pd = Py.Import("pandas");
            dynamic series = pd.Series(data, index: index, dtype: dtype, name: name);

            // Here, you can perform operations on the series or just return it.
            // For example, just returning the series object:
            return series;
        }
    }

    public void CastSeriesType(string seriesName, string dtype)
        {
            using (Py.GIL())
            {
                string script = $"{seriesName}.astype('{dtype}')";
                RunPythonScript(script, null);
            }
        }
        public void SumSeries(string seriesName)
        {
            using (Py.GIL())
            {
                string script = $"{seriesName}.sum()";
                RunPythonScript(script, null);
            }
        }
        public void MeanSeries(string seriesName)
        {
            using (Py.GIL())
            {
                string script = $"{seriesName}.mean()";
                RunPythonScript(script, null);
            }
        }
        public void DetectMissingValuesSeries(string seriesName)
        {
            using (Py.GIL())
            {
                string script = $"{seriesName}.isna()"; // or .isnull()
                RunPythonScript(script, null);
            }
        }
        public void SeriesToNumpyArray(string seriesName)
        {
            using (Py.GIL())
            {
                string script = $"{seriesName}.to_numpy()";
                RunPythonScript(script, null);
            }
        }
        public void SeriesIdxMax(string seriesName)
        {
            using (Py.GIL())
            {
                string script = $"{seriesName}.idxmax()";
                RunPythonScript(script, null);
            }
        }
        public void ValueCountsSeries(string seriesName)
        {
            using (Py.GIL())
            {
                string script = $"{seriesName}.value_counts()";
                RunPythonScript(script, null);
            }
        }

        public void SampleWithoutReplacement(string dataFrameName, int sampleSize, string newFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{newFrameName} = {dataFrameName}.sample(n={sampleSize}, replace=False)";
                PersistentScope.Exec(script);
            }
        }
        public string CompareDataFrames(string dataFrame1Name, string dataFrame2Name)
        {
            using (Py.GIL())
            {
                string script = $"diff = pd.concat([";
                script += $"{dataFrame1Name}, {dataFrame2Name}],";
                script += $"ignore_index=True).drop_duplicates(keep=False)";
                PersistentScope.Exec(script);

                // Return the DataFrame containing differences
                dynamic result = PersistentScope.Eval("diff");
                return result.ToString();
            }
        }
        public string AnomalyDetectionIsolationForest(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"from sklearn.ensemble import IsolationForest\n";
                script += $"clf = IsolationForest(contamination=0.05)  # Adjust contamination as needed\n";
                script += $"X = {dataFrameName}[['{columnName}']]\n";
                script += $"clf.fit(X)\n";
                script += $"outliers = {dataFrameName}[clf.predict(X) == -1]\n";

                // Return the DataFrame containing outliers/anomalies
                dynamic result = PersistentScope.Eval("outliers");
                return result.ToString();
            }
        }
        public string CompareDataFramesSchema(string dataFrame1Name, string dataFrame2Name)
        {
            using (Py.GIL())
            {
                string script = $"schema1 = {dataFrame1Name}.dtypes.to_dict()\n";
                script += $"schema2 = {dataFrame2Name}.dtypes.to_dict()\n";
                script += $"diff = {{'Column': [], 'In DataFrame 1': [], 'In DataFrame 2': []}}\n";

                // Compare columns from DataFrame 1 with DataFrame 2
                script += $"for col in schema1:\n";
                script += $"    if col not in schema2:\n";
                script += $"        diff['Column'].append(col)\n";
                script += $"        diff['In DataFrame 1'].append(str(schema1[col]))\n";
                script += $"        diff['In DataFrame 2'].append('Not Found')\n";

                // Compare columns from DataFrame 2 with DataFrame 1
                script += $"for col in schema2:\n";
                script += $"    if col not in schema1:\n";
                script += $"        diff['Column'].append(col)\n";
                script += $"        diff['In DataFrame 1'].append('Not Found')\n";
                script += $"        diff['In DataFrame 2'].append(str(schema2[col]))\n";

                // Create a DataFrame from the comparison result
                script += $"comparison_result = pd.DataFrame(diff)\n";

                // Return the DataFrame containing schema differences
                dynamic result = PersistentScope.Eval("comparison_result");
                return result.ToString();
            }
        }
        public string MergeDataFramesWithDifferenceTracking(string dataFrame1Name, string dataFrame2Name, string onKey, string how)
        {
            using (Py.GIL())
            {
                // Merge DataFrames
                string mergeScript = $"merged_df = pd.merge({dataFrame1Name}, {dataFrame2Name}, on='{onKey}', how='{how}', suffixes=('_df1', '_df2'))";

                // Track added, updated, and deleted rows
                string trackScript = "added_rows = merged_df[merged_df['_df1'].isnull()]\n";
                trackScript += "updated_rows = merged_df[merged_df['_df1'].notnull() & merged_df['_df2'].notnull()]\n";
                trackScript += "deleted_rows = merged_df[merged_df['_df2'].isnull()]\n";

                // Evaluate the scripts
                PersistentScope.Exec($"{mergeScript}\n{trackScript}");

                // Convert the tracked rows to string representations
                dynamic addedRows = PersistentScope.Eval("added_rows.to_string(index=False)");
                dynamic updatedRows = PersistentScope.Eval("updated_rows.to_string(index=False)");
                dynamic deletedRows = PersistentScope.Eval("deleted_rows.to_string(index=False)");

                return $"Added Rows:\n{addedRows}\n\nUpdated Rows:\n{updatedRows}\n\nDeleted Rows:\n{deletedRows}";
            }
        }
        public string CosineSimilarity(string dataFrameName, int row1Index, int row2Index)
        {
            using (Py.GIL())
            {
                string script = $"from sklearn.metrics.pairwise import cosine_similarity\n";
                script += $"row1 = {dataFrameName}.iloc[{row1Index}].values.reshape(1, -1)\n";
                script += $"row2 = {dataFrameName}.iloc[{row2Index}].values.reshape(1, -1)\n";
                script += $"similarity_score = cosine_similarity(row1, row2)[0][0]\n";

                // Return the cosine similarity score
                dynamic result = PersistentScope.Eval($"{script}");
                return result.ToString();
            }
        }
        public string CalculateCorrelation(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"correlation_matrix = {dataFrameName}.corr()\n";

                // Return the correlation matrix as a DataFrame
                dynamic result = PersistentScope.Eval($"{script}");
                return result.ToString();
            }
        }
        public string CalculateCovariance(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"covariance_matrix = {dataFrameName}.cov()\n";

                // Return the covariance matrix as a DataFrame
                dynamic result = PersistentScope.Eval($"{script}");
                return result.ToString();
            }
        }
        public string GetUniqueValues(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"unique_values = {dataFrameName}['{columnName}'].unique()\n";

                // Return the array of unique values
                dynamic result = PersistentScope.Eval($"{script}");
                return result.ToString();
            }
        }
        public string GetValueCounts(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"value_counts = {dataFrameName}['{columnName}'].value_counts()\n";

                // Return the Series with value counts
                dynamic result = PersistentScope.Eval($"{script}");
                return result.ToString();
            }
        }
        public string CalculateFrequencyDistribution(string dataFrameName, string columnName)
        {
            using (Py.GIL())
            {
                string script = $"frequency_distribution = {dataFrameName}['{columnName}'].value_counts()\n";

                // Return the frequency distribution as a Series
                dynamic result = PersistentScope.Eval($"{script}");
                return result.ToString();
            }
        }
        public void DisplayDataFrame(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}";
                PersistentScope.Exec(script);
            }
        }
        public string GetSummaryStatistics(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.describe()";
                dynamic result = PersistentScope.Eval(script);
                return result.ToString();
            }
        }
        public string SelectColumns(string dataFrameName, List<string> columns)
        {
            using (Py.GIL())
            {
                string columnList = string.Join(", ", columns.Select(c => $"'{c}'"));
                string script = $"{dataFrameName}[{columnList}]";
                dynamic result = PersistentScope.Eval(script);
                return result.ToString();
            }
        }
        public string FilterDataFrame(string dataFrameName, string condition)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}[{condition}]";
                dynamic result = PersistentScope.Eval(script);
                return result.ToString();
            }
        }
        // Function to get the count of non-null/missing values in each column
        public void CountNonNullValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.count()";
                RunPythonScript(script, null);
            }
        }

        // Function to get the sum of values in each column
        public void SumValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.sum()";
               RunPythonScript(script, null);
            }
        }
        // Function to get the mean of values in each column
        public void MeanValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.mean()";
                RunPythonScript(script, null);
            }
        }

        // Function to get the maximum value in each column
        public void MaxValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.max()";
                RunPythonScript(script, null);
            }
        }

        // Function to get the minimum value in each column
        public void MinValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.min()";
                RunPythonScript(script, null);
            }
        }

        // Function to get the median of values in each column
        public void MedianValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.median()";
                RunPythonScript(script, null);
            }
        }

        // Function to get the variance of values in each column
        public void VarianceValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.var()";
                RunPythonScript(script, null);
            }
        }

        // Function to get the standard deviation of values in each column
        public void StdDeviationValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.std()";
                RunPythonScript(script, null);
            }
        }
        public void DescribeValues(string dataFrameName)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.describe()";
                RunPythonScript(script, null);
            }
        }
        public void MeltDataFrame(string dataFrameName, string idVars = null, string valueVars = null)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.melt(id_vars={idVars}, value_vars={valueVars})";
                RunPythonScript(script, null);
            }
        }
        public void PivotDataFrame(string dataFrameName, string columns, string index = null, string values = null)
        {
            using (Py.GIL())
            {
                string script = $"{dataFrameName}.pivot(index='{index}', columns='{columns}', values='{values}')";
                RunPythonScript(script, null);
            }
        }

        #endregion "Pandas DataFrame"
    }
}
