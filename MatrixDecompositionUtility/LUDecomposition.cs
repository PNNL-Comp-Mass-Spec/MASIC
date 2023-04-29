using System;
using System.Collections.Generic;

namespace MatrixDecompositionUtility
{
    public class LUDecomposition
    {
        // Ignore Spelling: ludcmp, lubksb

        public double[] ProcessData(double[,] a, int n, double[] b)
        {
            var index = new int[n];

            var matrixA = (double[,])a.Clone();
            var matrixB = (double[])b.Clone();

            // Solve the system of linear equations by first calling ludcmp and then calling lubksb
            // The goal is to solve for X in A.X = B
            // Solve, to get X = B Inverse A

            // First invert matrix A
            ludcmp(matrixA, n, index);

            // Now multiply inverted A by B
            lubksb(matrixA, n, index, matrixB);

            // Return the results
            return matrixB;
        }

        /// <summary>
        /// Linear equation solution, back substitution
        /// </summary>
        /// <remarks>From Numerical Recipes in C</remarks>
        /// <param name="a"></param>
        /// <param name="n"></param>
        /// <param name="index"></param>
        /// <param name="b"></param>
        private void lubksb(double[,] a, int n, IReadOnlyList<int> index, IList<double> b)
        {
            int i;
            int j;
            double sum;
            var ii = -1;

            for (i = 0; i < n; i++)
            {
                var ip = index[i];
                sum = b[ip];
                b[ip] = b[i];

                if (ii > -1)
                {
                    j = ii;

                    while (j < i)
                    {
                        sum -= a[i, j] * b[j];
                        j++;
                    }
                }
                else if (sum > 0.0)
                {
                    ii = i;
                }
                b[i] = sum;
            }

            for (i = n - 1; i >= 0; i--)
            {
                sum = b[i];

                for (j = i + 1; j < n; j++)
                {
                    sum -= a[i, j] * b[j];
                }

                b[i] = sum / a[i, i];
            }
        }

        /// <summary>
        /// Linear equation solution, LU decomposition
        /// </summary>
        /// <remarks>From Numerical Recipes in C</remarks>
        /// <param name="a"></param>
        /// <param name="n"></param>
        /// <param name="index"></param>
        private void ludcmp(double[,] a, int n, IList<int> index)
        {
            int j;
            double big;
            var iMax = 0;
            var vv = new double[n];
            var d = 1.0;
            var i = 0;

            while (i < n)
            {
                big = 0.0;
                j = 0;

                while (j < n)
                {
                    double temp;

                    if ((temp = Math.Abs(a[i, j])) > big)
                    {
                        big = temp;
                    }
                    j++;
                }

                if (Math.Abs(big) < double.Epsilon)
                {
                    Console.WriteLine("Singular matrix in method ludcmp!!");
                }

                vv[i] = 1.0 / big;
                i++;
            }

            for (j = 0; j < n; j++)
            {
                int k;
                double temp;
                double sum;
                i = 0;

                while (i < j)
                {
                    sum = a[i, j];
                    k = 0;

                    while (k < i)
                    {
                        sum -= a[i, k] * a[k, j];
                        k++;
                    }

                    a[i, j] = sum;
                    i++;
                }

                big = 0.0;
                i = j;

                while (i < n)
                {
                    sum = a[i, j];
                    k = 0;

                    while (k < j)
                    {
                        sum -= a[i, k] * a[k, j];
                        k++;
                    }
                    a[i, j] = sum;

                    if ((temp = vv[i] * Math.Abs(sum)) >= big)
                    {
                        big = temp;
                        iMax = i;
                    }
                    i++;
                }

                if (j != iMax)
                {
                    for (k = 0; k < n; k++)
                    {
                        temp = a[iMax, k];
                        a[iMax, k] = a[j, k];
                        a[j, k] = temp;
                    }

                    d = -d;
                    vv[iMax] = vv[j];
                }

                index[j] = iMax;

                if (Math.Abs(a[j, j]) < double.Epsilon)
                {
                    a[j, j] = double.MinValue;
                }

                if (j != n)
                {
                    temp = 1.0 / a[j, j];

                    for (i = j + 1; i < n; i++)
                    {
                        a[i, j] *= temp;
                    }
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void PrintData(double[,] a, int n, IReadOnlyList<int> index, IReadOnlyList<double> b)
        {
            for (var i = 0; i < n; i++)
            {
                for (var m = 0; m < n; m++)
                {
                    Console.Write("{0}\t", a[i, m]);
                }
                Console.WriteLine("");
            }

            Console.WriteLine("N is {0}", n);
            Console.Write("index[]: ");

            for (var j = 0; j < n; j++)
            {
                Console.Write("{0}\t", index[j]);
            }

            Console.WriteLine("");
            Console.Write("B matrix: ");

            for (var k = 0; k < n; k++)
            {
                Console.Write("{0}\t", b[k]);
            }

            Console.WriteLine("");
        }
    }
}
