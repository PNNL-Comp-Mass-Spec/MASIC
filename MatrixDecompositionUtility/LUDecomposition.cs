using System;

namespace MatrixDecompositionUtility
{
    public class LUDecomposition
    {

        public double[] ProcessData(double[,] a, int n, double[] b) {
            var indx = new int[n];

            var MatrixA = (double[,])a.Clone();
            var MatrixB = (double[])b.Clone();

            // Solve the system of linear equations by first calling ludcmp and then calling lubksb
            // The goal is to solve for X in A.X = B
            // Solve, to get X = B Inverse A

            // First invert matrix A
            this.ludcmp(MatrixA, n, indx);

            // Now multiply inverted A by B
            this.lubksb(MatrixA, n, indx, MatrixB);

            // Return the results
            return MatrixB;
        }

        // Linear equation solution, back substitution
        private void lubksb(double[,] a, int n, int[] indx, double[] b) {
            int i;
            int j;
            double sum;
            var ii = -1;
            for (i = 0; i < n; i++) {
                var ip = indx[i];
                sum = b[ip];
                b[ip] = b[i];
                if (ii > -1) {
                    j = ii;
                    while (j <= (i - 1)) {
                        sum -= a[i, j] * b[j];
                        j++;
                    }
                }
                else if (sum > 0.0) {
                    ii = i;
                }
                b[i] = sum;
            }
            for (i = n - 1; i >= 0; i--) {
                sum = b[i];
                for (j = i + 1; j < n; j++) {
                    sum -= a[i, j] * b[j];
                }
                b[i] = sum / a[i, i];
            }
        }

        // Linear equation solution, LU decomposition
        private void ludcmp(double[,] a, int n, int[] indx) {
            int j;
            double big;
            var imax = 0;
            var vv = new double[n];
            var d = 1.0;
            var i = 0;
            while (i < n) {
                big = 0.0;
                j = 0;
                while (j < n) {
                    double temp;
                    if ((temp = Math.Abs(a[i, j])) > big) {
                        big = temp;
                    }
                    j++;
                }
                if (Math.Abs(big) < double.Epsilon) {
                    Console.WriteLine("Singular matrix in routing ludcmp!!");
                }
                vv[i] = 1.0 / big;
                i++;
            }
            for (j = 0; j < n; j++) {
                int k;
                double dum;
                double sum;
                i = 0;
                while (i < j) {
                    sum = a[i, j];
                    k = 0;
                    while (k < i) {
                        sum -= a[i, k] * a[k, j];
                        k++;
                    }
                    a[i, j] = sum;
                    i++;
                }
                big = 0.0;
                i = j;
                while (i < n) {
                    sum = a[i, j];
                    k = 0;
                    while (k < j) {
                        sum -= a[i, k] * a[k, j];
                        k++;
                    }
                    a[i, j] = sum;
                    if ((dum = vv[i] * Math.Abs(sum)) >= big) {
                        big = dum;
                        imax = i;
                    }
                    i++;
                }
                if (j != imax) {
                    for (k = 0; k < n; k++) {
                        dum = a[imax, k];
                        a[imax, k] = a[j, k];
                        a[j, k] = dum;
                    }
                    d = -d;
                    vv[imax] = vv[j];
                }
                indx[j] = imax;
                if (Math.Abs(a[j, j]) < double.Epsilon) {
                    a[j, j] = double.MinValue;
                }
                if (j != n) {
                    dum = 1.0 / a[j, j];
                    for (i = j + 1; i < n; i++) {
                        a[i, j] *= dum;
                    }
                }
            }
        }

        private void printData(double[,] a, int n, int[] indx, double[] b) {
            for (var i = 0; i < n; i++) {
                for (var m = 0; m < n; m++) {
                    Console.Write("{0}\t", a[i, m]);
                }
                Console.WriteLine("");
            }
            Console.WriteLine("N is {0}", n);
            Console.Write("indx[]: ");
            for (var j = 0; j < n; j++) {
                Console.Write("{0}\t", indx[j]);
            }
            Console.WriteLine("");
            Console.Write("B matrix: ");
            for (var k = 0; k < n; k++) {
                Console.Write("{0}\t", b[k]);
            }
            Console.WriteLine("");
        }

    }
}
