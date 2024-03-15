using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Newtonsoft.Json;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Beep.Python.RuntimeEngine
{


    public class PythonPlotManager : PythonBaseViewModel
    {
        public PythonPlotManager(PythonNetRunTimeManager pythonRuntimeManager, PyModule persistentScope) : base(pythonRuntimeManager, persistentScope)
        {
     
        }
        public PythonPlotManager(PythonNetRunTimeManager pythonRuntimeManager) : base(pythonRuntimeManager)
        {
          
            InitializePythonEnvironment();
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
                // Create a new figure with specified size and DPI
                plt.figure(figsize: np.array(new double[] { 800, 600 }), dpi: 100);

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
                // plt.show();
                plt.savefig(picfile);
                // Close the figure to free up memory
                plt.close();
            }
        }
        public void GenerateContourPlot(ContourPlotData data)
        {
            // Check if the object is not null and if initialization is needed
            if (data == null || !IsInitialized)
            {
                Console.WriteLine("Data is null or system not initialized.");
                return;
            }
            // Serialize the data to JSON
            //string jsonData = JsonConvert.SerializeObject(new
            //{
            //    x = data.x,
            //    y = data.y,
            //    z = data.z
            //});
            //   string jsonData = JsonConvert.SerializeObject(data);
            string modifiedFilePath = data.picfile.Replace("\\", "/");

            string jsonData = JsonConvert.SerializeObject(new
            {
                x = data.x,
                y = data.y,
                z = data.z,
                picfile = modifiedFilePath // Ensure this is included if the file path is part of the JSON data
            });
            // Python script with placeholders for data
            string script = $@"
import matplotlib.pyplot as plt
import numpy as np
from numpy import ma
from matplotlib import cm, ticker
import json

# Received JSON data
jsonData = '''{jsonData}'''

# Deserialize JSON data to Python objects
data = json.loads(jsonData)
x = np.array(data['x'])
y = np.array(data['y'])
z = np.array(data['z']) # Assuming z is 2D and needs reshaping

X, Y = np.meshgrid(x, y)
Z = ma.masked_where(z <= 0, z)

fig, ax = plt.subplots(figsize=(100,60))
cs = ax.contourf(X, Y, Z,levels =10, cmap=cm.PuBu_r)
cbar = fig.colorbar(cs)
plt.savefig(r'{modifiedFilePath}',dpi=300)
plt.close()
";
            // Run the Python script
            RunPythonScript(script, null);
        }
        public void GenerateContourPlotIrregular(ContourPlotData data)
        {
            // Check if the object is not null and if initialization is needed
            if (data == null || !IsInitialized)
            {
                Console.WriteLine("Data is null or system not initialized.");
                return;
            }
            // Serialize the data to JSON
            //string jsonData = JsonConvert.SerializeObject(new
            //{
            //    x = data.x,
            //    y = data.y,
            //    z = data.z
            //});
            //   string jsonData = JsonConvert.SerializeObject(data);
            string modifiedFilePath = data.picfile.Replace("\\", "/");

            string jsonData = JsonConvert.SerializeObject(new
            {
                x = data.x,
                y = data.y,
                z = data.z,
                picfile = modifiedFilePath // Ensure this is included if the file path is part of the JSON data
            });
            // Python script with placeholders for data
            string script = $@"import matplotlib.pyplot as plt
import numpy as np
import matplotlib.tri as tri
from matplotlib import cm, ticker
import json

np.random.seed(19680801)
npts = 200
ngridx = 100
ngridy = 200
# Received JSON data
jsonData = '''{jsonData}'''

# Deserialize JSON data to Python objects
data = json.loads(jsonData)
x = np.array(data['x'])
y = np.array(data['y'])
z = np.array(data['z']) # Assuming z is 2D and needs reshaping


fig, (ax1, ax2) = plt.subplots(nrows=2)

# -----------------------
# Interpolation on a grid
# -----------------------
# A contour plot of irregularly spaced data coordinates
# via interpolation on a grid.

# Calculate the bounds of your data
x_min, x_max = np.min(x), np.max(x)
y_min, y_max = np.min(y), np.max(y)

# Optionally, expand the bounds a little to ensure the grid covers all points
margin_x = (x_max - x_min) * 0.05  # 5% of the range, adjust as necessary
margin_y = (y_max - y_min) * 0.05

# Create grid values with a slight margin
xi = np.linspace(x_min - margin_x, x_max + margin_x, num=100)  # Adjust num for grid resolution
yi = np.linspace(y_min - margin_y, y_max + margin_y, num=100)

# Linearly interpolate the data (x, y) on a grid defined by (xi, yi).
triang = tri.Triangulation(x, y)
interpolator = tri.LinearTriInterpolator(triang, z)
Xi, Yi = np.meshgrid(xi, yi)
zi = interpolator(Xi, Yi)

# Note that scipy.interpolate provides means to interpolate data on a grid
# as well. The following would be an alternative to the four lines above:
# from scipy.interpolate import griddata
# zi = griddata((x, y), z, (xi[None, :], yi[:, None]), method='linear')

ax1.contour(xi, yi, zi, levels=14, linewidths=0.5, colors='k')
cntr1 = ax1.contourf(xi, yi, zi, levels=14, cmap=""RdBu_r"")

fig.colorbar(cntr1, ax=ax1)
ax1.plot(x, y, 'ko', ms=3)
ax1.set(xlim=(-2, 2), ylim=(-2, 2))
ax1.set_title('grid and contour (%d points, %d grid points)' %
              (npts, ngridx * ngridy))

# ----------
# Tricontour
# ----------
# Directly supply the unordered, irregularly spaced coordinates
# to tricontour.

ax2.tricontour(x, y, z, levels=14, linewidths=0.5, colors='k')
cntr2 = ax2.tricontourf(x, y, z, levels=14, cmap=""RdBu_r"")

fig.colorbar(cntr2, ax=ax2)
ax2.plot(x, y, 'ko', ms=3)
ax2.set(xlim=(-2, 2), ylim=(-2, 2))
ax2.set_title('tricontour (%d points)' % npts)

plt.subplots_adjust(hspace=0.5)
plt.savefig(r'{modifiedFilePath}',dpi=300)
plt.close()
";
            // Run the Python script
            RunPythonScript(script, null);
        }
        public void CreatePyPlotTriContourPlot(ContourPlotData data, bool useLogScale = true)
        {
            if (!IsInitialized)
            {
                return;
            }

            using (Py.GIL())
            {
                // create a Python scope
                using (PyModule scope = Py.CreateScope())
                {
                    dynamic plt = Py.Import("matplotlib.pyplot");
                    dynamic np = Py.Import("numpy");
                    dynamic mpl = Py.Import("matplotlib");
                    dynamic colors = Py.Import("matplotlib.colors");

                    // convert the Person object to a PyObject
                    PyObject pydata = data.ToPython();
                    scope.Set("data", pydata);
                    //// convert the Person object to a PyObject
                    //PyObject pyPerson = person.ToPython();
                    //// convert the Person object to a PyObject
                    //PyObject pyPerson = person.ToPython();

                    // Convert your C# arrays to NumPy arrays
                    dynamic x_np = np.array(data.x);
                    dynamic y_np = np.array(data.y);
                    dynamic z_np = np.array(data.z).flatten(); // Assuming FlattenZArray correctly flattens your 2D z array to 1D

                    // Define vmin and vmax for the color scale, especially important for logarithmic scaling
                    double vmin = np.min(z_np);
                    double vmax = np.max(z_np);

                    // Adjust levels based on whether you're using a logarithmic scale
                    dynamic levels;
                    if (useLogScale)
                    {
                        vmin = vmin <= 0 ? 0.1 : vmin; // Ensure vmin is positive and non-zero for log scale
                        levels = np.logspace(np.log10(vmin), np.log10(vmax), 12);
                    }
                    else
                    {
                        levels = np.linspace(vmin, vmax, 12);
                    }

                    dynamic norm = useLogScale ? colors.LogNorm(vmin = vmin, vmax = vmax) : null;

                    // Check for invalid values to ensure plotting can proceed
                    bool hasInvalidValues = data.x.Any(double.IsNaN) || data.y.Any(double.IsNaN) || z_np.any(np.isnan) ||
                                            data.x.Any(double.IsInfinity) || data.y.Any(double.IsInfinity) || z_np.any(np.isinf);
                    if (hasInvalidValues)
                    {
                        Console.WriteLine("Arrays contain NaN or Infinite values.");
                        return;
                    }
                    // Create the contour plot with logarithmic scaling

                    // Create the contour plot with or without logarithmic scaling
                    if (useLogScale)
                    {
                        plt.contourf(x_np, y_np, z_np, levels = levels, norm = norm);
                    }
                    else
                    {
                        plt.contourf(x_np, y_np, z_np, levels = levels);
                    }

                    plt.xlabel(data.xLabel);
                    plt.ylabel(data.yLabel);
                    plt.title(data.title);
                    plt.colorbar();

                    // Optionally add labels to the contour lines
                    // plt.clabel(contour, inline=True, fontsize=8); // Uncomment if you wish to add labels to the lines

                    // Add titles at specific x, y points
                    foreach (var pointTitle in data.pointTitles)
                    {
                        double pointX = pointTitle.Item1;
                        double pointY = pointTitle.Item2;
                        string label = pointTitle.Item3;
                        plt.text(pointX, pointY, label, fontsize: 12, color: "red");
                    }

                    // Save the plot
                    plt.savefig(data.picfile);

                    // Close the figure to free up memory
                    plt.close();

                }
             
            }

        }
        public void CreateplotlyContourPlotScript(ContourPlotData data)
        {
            if (!IsInitialized)
            {
                return;
            }

            int rows = 100; // Determine based on your data structure
            int cols = 100; // Determine based on your data structure

            // Generate the 2D array for Z values
            var zData = GenerateZDataForPlotly(data, rows, cols);
            string modifiedFilePath = data.picfile.Replace("\\", "/");
            // Serialize the Z data to JSON
            string zJson = SerializeZDataForPlotly(zData);

            // Now use the serialized JSON in your Python script
            string script = $@"
import plotly.graph_objs as go
import json

# Your serialized Z data
z_json = '''{zJson}'''

# Deserialize the Z data from JSON
z_data = json.loads(z_json)

# Create the contour plot with Plotly
fig = go.Figure()
fig.add_trace(go.Contour(z=z, showscale=False, connectgaps=True))
fig.update_layout(title: '{data.title}',
                  xaxis_title: '{data.xLabel}',
                  yaxis_title: '{data.yLabel}')
fig.write_image(r'{modifiedFilePath}')
";

            // Run the Python script
            RunPythonScript(script, null);
        }

        public void CreateplotlyContourPlot(ContourPlotData data)
        {
            if (!IsInitialized)
            {
                return;
            }
            int rows = 100; // Determine based on your data structure
            int cols = 100; // Determine based on your data structure
            // Generate the 2D array for Z values
       //     var zData = GenerateZDataForPlotly(data, rows, cols);

            // Serialize the Z data to JSON
          //  string zJson = SerializeZDataForPlotly(zData);
            using (Py.GIL())
            {
                // create a Python scope
                using (PyModule scope = Py.CreateScope())
                {
                    dynamic plotly = Py.Import("plotly");
                    dynamic go = Py.Import("plotly.graph_objects");
                    dynamic np = Py.Import("numpy");
                    // convert the Person object to a PyObject
                    PyObject pydata = data.ToPython();
                    scope.Set("data", pydata);
                    //// convert the Person object to a PyObject
                    //PyObject pyPerson = person.ToPython();
                    //// convert the Person object to a PyObject
                    //PyObject pyPerson = person.ToPython();

                    // Convert your C# arrays to NumPy arrays
                   
                    dynamic x_np = np.array(data.x);
                    dynamic y_np = np.array(data.y);
                    dynamic z_np = np.array(data.z); // Assuming FlattenZArray correctly flattens your 2D z array to 1D

                    // Create a new figure
                    dynamic fig = go.Figure();
                    // Add contour plot to the figure
                    fig.add_trace(go.Contour(
                        x: x_np,
                        y: y_np,
                        z: z_np,
                        colorscale: "Viridis" // You can choose a colorscale e.g., "Viridis", "Cividis", "Blues", etc.
                    ));
                    // Set layout properties
                    fig.update_layout(
                        title: data.title,
                        xaxis_title: data.xLabel,
                        yaxis_title: data.yLabel
                    );

                    // Save the plot
                    fig.write_image(data.picfile);

                    // Close the figure to free up memory
                   // fig.close();
                }
               
            }
        }
        public string SerializeZDataForPlotly(double?[,] zData)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
            };
            return JsonConvert.SerializeObject(zData, settings);
        }

        public double?[,] GenerateZDataForPlotly(ContourPlotData data, int rows, int cols)
        {
            // Create a new 2D array for Z values, initializing all to null
            var z = new double?[rows, cols];

            // Assuming data.z is a 1D array of length rows*cols
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    int index = i * cols + j;
                    if (index < data.z.Length)
                    {
                        // Assign the value from data.z to the 2D array
                        z[i, j] = data.z[index];
                    }
                }
            }
            return z;
        }

        // You need to implement FlattenZArray if you have not already
        // It should take a 2D array and flatten it to a 1D array
        private double[] FlattenZArray(double[,] z)
        {
            int width = z.GetLength(0);
            int height = z.GetLength(1);
            double[] z1D = new double[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    z1D[x * height + y] = z[x, y];
                }
            }
            return z1D;
        }

    }
}
