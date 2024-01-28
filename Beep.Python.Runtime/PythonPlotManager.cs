using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine
{
    public enum ChartType
    {
        LinePlot,       // Line plot for trends
        BarChart,       // Bar chart for comparing categories/values
        Histogram,      // Histogram for data distribution
        PieChart,       // Pie chart for proportional data
        BoxPlot,        // Box plot for displaying statistical data
        Heatmap,        // Heatmap for matrix-like data
        AreaPlot,       // Area plot for filled area under a curve
        ViolinPlot,     // Violin plot for displaying distribution of data
        BoxenPlot,      // Boxen plot for visualizing large datasets
        HexbinPlot,     // Hexbin plot for 2D binning of data points
        ContourPlot,    // Contour plot for 2D data with contour lines
        ScatterPlot,    // Scatter plot for visualizing the relationship between two variables
        PolarPlot,      // Polar plot for data with circular representation
        SpiderPlot,     // Spider plot for multivariate data
        ErrorBarPlot,   // Error bar plot for displaying uncertainties
        QuiverPlot,     // Quiver plot for vector field data
        StreamPlot,     // Stream plot for displaying flow or streamlines
        Bar3DPlot,      // 3D bar chart
        Scatter3DPlot,  // 3D scatter plot
        BubbleChart,    // Bubble chart for 2D data with varying bubble sizes
        SankeyDiagram,  // Sankey diagram for flow or process visualization
        TreeMap,        // TreeMap for hierarchical data visualization
        WordCloud,      // Word cloud for text data visualization
        ParallelCoordinatesPlot, // Parallel coordinates plot for multivariate data
        BarPlot,
        PairPlot
    }

    public class PythonPlotManager : IDisposable
    {
        private readonly PythonNetRunTimeManager _pythonRuntimeManager;
        private PyModule _persistentScope;
        public PythonPlotManager(PythonNetRunTimeManager pythonRuntimeManager)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            InitializePythonEnvironment();
        }
        public void ImportPythonModule(string moduleName)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import {moduleName}";
            RunPythonScript(script, null);
        }
        public bool IsInitialized => _pythonRuntimeManager.IsInitialized;
        private bool InitializePythonEnvironment()
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)
            {
                _pythonRuntimeManager.Initialize();
            }
            if (!_pythonRuntimeManager.IsInitialized)
            {
                return retval;
            }
            using (Py.GIL())
            {
                _persistentScope = Py.CreateScope("__main__");
                _persistentScope.Exec("models = {}");  // Initialize the models dictionary
                retval = true;
            }
            return retval;
        }
        public void CreateSeabornChart(string picfile, double[] data, string xLabel, string yLabel, string title, ChartType chartType)
        {
            if (!IsInitialized)
            {
                return;
            }

            using (Py.GIL())
            {
                dynamic sns = Py.Import("seaborn");
                dynamic plt = Py.Import("matplotlib.pyplot");

                // Create a Seaborn plot
                sns.set(style: "whitegrid"); // You can set the Seaborn style as needed

                switch (chartType)
                {
                    case ChartType.BarPlot:
                        sns.barplot(x: data);
                        break;

                    case ChartType.ScatterPlot:
                        sns.scatterplot(x: data);
                        break;

                    case ChartType.Histogram:
                        sns.histplot(data, kde: true);
                        break;

                    case ChartType.LinePlot:
                        sns.lineplot(x: data);
                        break;

                    case ChartType.BoxPlot:
                        sns.boxplot(x: data);
                        break;

                    case ChartType.ViolinPlot:
                        sns.violinplot(x: data);
                        break;

                    case ChartType.PairPlot:
                        // Create a pair plot with a DataFrame (example)
                        dynamic pd = Py.Import("pandas");
                        dynamic df = pd.DataFrame(data);
                        sns.pairplot(df);
                        break;

                    case ChartType.Heatmap:
                        // Create a heatmap with a DataFrame (example)
                        dynamic pd2 = Py.Import("pandas");
                        dynamic df2 = pd2.DataFrame(data);
                        sns.heatmap(df2);
                        break;

                    // Add more cases for other chart types as needed

                    default:
                        throw new ArgumentException("Invalid chart type");
                }

                plt.xlabel(xLabel);
                plt.ylabel(yLabel);
                plt.title(title);

                // Show the plot
                plt.show();
                plt.savefig(picfile);
            }
        }
        public void CreatePyPlotChart(string picfile, double[] x, double[] y, string title, string xLabel, string yLabel, ChartType chartType, int width, int height)
        {
            if (!IsInitialized)
            {
                return;
            }

            using (Py.GIL())
            {
                dynamic plt = Py.Import("matplotlib.pyplot");
                // Set the figure size
                //plt.figure(figsize: );
                switch (chartType)
                {
                    case ChartType.LinePlot:
                        plt.plot(x, y);
                        break;
                    case ChartType.ScatterPlot:
                        plt.scatter(x, y);
                        break;
                    case ChartType.BarChart:
                        plt.bar(x, y);
                        break;
                    case ChartType.Histogram:
                        plt.hist(x, bins: y.Length);
                        break;
                    case ChartType.PieChart:
                        plt.pie(x, labels: y);
                        break;
                    case ChartType.BoxPlot:
                        plt.boxplot(x);
                        break;
                    //case ChartType.Heatmap:
                    //    plt.imshow(y, cmap: "viridis", extent: [x[0], x[x.Length - 1], y[0], y[y.Length - 1]]);

                    //    plt.colorbar();
                    //    break;
                    case ChartType.AreaPlot:
                        plt.fill_between(x, y);
                        break;
                    case ChartType.ViolinPlot:
                        plt.violinplot(x);
                        break;
                    case ChartType.BoxenPlot:
                        plt.boxenplot(x);
                        break;
                    //case ChartType.HexbinPlot:
                    //    plt.hexbin(x, y, gridsize: 30, cmap: "Blues");
                    //    break;
                    //case ChartType.ContourPlot:
                    //    plt.contour(x, y, cmap: "viridis");
                    //    plt.colorbar();
                    //    break;
                    //case ChartType.Scatter3DPlot:
                    //    dynamic mplot3d = Py.Import("mpl_toolkits.mplot3d");
                    //    dynamic ax = plt.gca(projection: "3d");
                    //    ax.scatter(x, y, z);
                    //    break;
                    // Add more chart types as needed
                    default:
                        throw new ArgumentException("Invalid chart type");
                }

                plt.title(title);
                plt.xlabel(xLabel);
                plt.ylabel(yLabel);

                // Save or show the plot as needed
                plt.show();
                plt.savefig(picfile);
            }
        }
        public void CreatePyPlotContourPlot(string picfile, double[] x, double[] y, double[,] z, string xLabel, string yLabel, string title, List<Tuple<double, double, string>> pointTitles)
        {
            if (!IsInitialized)
            {
                return;
            }

            using (Py.GIL())
            {
                dynamic plt = Py.Import("matplotlib.pyplot");

                // Create a grid of x and y values using NumPy
                dynamic np = Py.Import("numpy");
                dynamic X = np.meshgrid(x, y);

                // Create a contour plot
                dynamic contour = plt.contour(X[0], X[1], z);
                plt.xlabel(xLabel);
                plt.ylabel(yLabel);
                plt.title(title);
                plt.colorbar();

                // Add labels to the contour lines
                plt.clabel(contour, inline: true, fontsize: 8);

                // Add titles at specific x, y points
                foreach (var pointTitle in pointTitles)
                {
                    double pointX = pointTitle.Item1;
                    double pointY = pointTitle.Item2;
                    string label = pointTitle.Item3;
                    plt.text(pointX, pointY, label, fontsize: 12, color: "red");
                }

                // Show the plot
                plt.show();
                plt.savefig(picfile);

            }
        }
        private void RunPythonScript(string script, dynamic parameters)
        {
            if (!IsInitialized)
            {
                return;
            }
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                _persistentScope.Exec(script); // Execute the script in the persistent scope
                                               // Handle outputs if needed

                // If needed, return results or handle outputs
            }
        }
        public void Dispose()
        {
            _persistentScope.Dispose();
            _pythonRuntimeManager.ShutDown();
        }
    }
}
