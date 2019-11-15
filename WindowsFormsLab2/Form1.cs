using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PLplot;

namespace WindowsFormsLab2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadTable();
            pictureBox1.ImageLocation = "white.png";
        }
        private DataTable _dt;
        private void LoadTable()
        {
            var dt = new DataTable();
            using (var sr = new StreamReader("data.csv", Encoding.Default))
            {
                var headers = sr.ReadLine().Split(';');
                foreach (var header in headers) dt.Columns.Add(header);

                while (!sr.EndOfStream)
                {
                    var rows = sr.ReadLine().Split(';');
                    var dr = dt.NewRow();
                    for (var i = 0; i < headers.Length; i++) dr[i] = rows[i];

                    dt.Rows.Add(dr);
                }
            }

            dataGridView1.DataSource = dt;
            _dt = dt;
        }
        private void button_save_Click(object sender, EventArgs e)
        {
            using (var sw = new StreamWriter("data.csv"))
            {
                sw.WriteLine("X;Y;P");
                foreach (DataRow row in _dt.Rows) sw.WriteLine("{0};{1};{2}", row[0], row[1], row[2]);
            }
        }

        private double SCR = 0.0;
        private void button_plot_Click(object sender, EventArgs e)
        {
            InitInput(out List<Double> X, out List<double> Y, out List<double> P, out int degree);

            double[,] A = new double[degree,degree];

            for(int i = 0; i < degree; i++)
                for (int j = 0; j < degree; j++)
                {
                    A[i,j] = PolySumA(i, j, X, P);
                }

            double[] B = new double[degree];

            for (int i = 0; i < degree; i++)
            {
                B[i] = PolySumB(i, X, Y, P);
            }

            double[] coef = new double[degree];
            coef = GaussMTB(A, B, degree);

            List<double> resultX = new List<double>();
            List<double> resultY = new List<double>();
            for (double x0 = X.First()-SCR; x0 < X.Last()+SCR; x0 = x0 + 0.1)
            {
                resultX.Add(x0);
                resultY.Add(y(coef, degree, x0));
            }
            
            using (PLStream pl = new PLStream())
            {
                pl.sdev("pngcairo");                // png rendering
                pl.sfnam("data.png");                 // output filename
                pl.spal0("cmap0_alternate.pal");   // alternate color palette
                //pl.col0(1);
                //pl.col0(100);

                pl.init();
                pl.env(X.Min()-SCR, X.Max()+SCR, Y.Min()-SCR, Y.Max()+SCR, AxesScale.Independent, AxisBox.BoxTicksLabelsAxes);
                pl.lab("x", "Plot", "Plot"); //y=3 x#u2#d

                pl.poin(X.ToArray(),Y.ToArray(), 'o');
                pl.line(resultX.ToArray(), resultY.ToArray());

                pl.eop();
            }
            pictureBox1.ImageLocation = "data.png";

        }

        private void InitInput(out List<double> X, out List<double> Y, out List<double> P, out int degree)
        {
            X = new List<double>();
            Y = new List<double>();
            P = new List<double>();

            degree = Int32.Parse(textBoxDegree.Text);

            foreach (DataRow row in _dt.Rows)
            {
                X.Add(Double.Parse(row[0].ToString()));
                Y.Add(Double.Parse(row[1].ToString()));
                P.Add(Double.Parse(row[2].ToString()));
            }
        }

        static double PolySumA(int i, int j, List<double> X, List<double> P)
        {
            double sum = 0;
            for (int k = 0; k < X.Count; k++)
            {
                sum += P[k] * Math.Pow(X[k], i + j);
            }
            return sum;
        }

        static double PolySumB(int i, List<double> X, List<double> Y, List<double> P)
        {
            double sum = 0;
            for (int j = 0; j < X.Count; j++)
            {
                sum += P[j] * Y[j] * Math.Pow(X[j], i);
            }
            return sum;
        }

        static double[] GaussMTB(double[,] z, double[] y, int n)
        {
            double[,] a = new double[n, n];
            Array.Copy(z, a, z.Length);
            double[] b = new double[n];
            Array.Copy(y, b, y.Length);

            //PrintM("GaussM", a, b, n);

            //Часть 1
            //Модифицированная верхнее приобразование (получение нижних нулей.
            for (int k = 0; k < n; k++)
            {
                double d = a[k, k];
                for (int j = k; j < n; j++)
                    a[k, j] /= d;
                b[k] /= d;
                for (int i = k + 1; i < n; i++)
                {
                    double r = a[i, k];
                    for (int j = k; j < n; j++)
                        a[i, j] -= a[k, j] * r;
                    b[i] -= b[k] * r;
                }
            }


            //Часть 2
            for (int k = n - 1; k > 0; k--)
            {
                for (int i = k - 1; i >= 0; i--)
                {
                    b[i] -= b[k] * a[i, k];
                    a[i, k] = 0.0;
                }
            }

            return b;
            //PrintM("GaussMTB", a, b, n);
        }

        static double y (double[] coefficients, int degree, double x)
        {
            double result = 0;
            for (int i = 0; i < degree; i++)
            {
                result += coefficients[i] * Math.Pow(x,i);
            }

            return result;
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
                SCR += 10;
            if (e.Button == MouseButtons.Right)
                SCR -= 10;
            button_plot_Click(this,e);
        }
    }
}
