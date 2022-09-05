using System;

namespace MASIC
{
    /// <summary>
    /// <para>
    /// This class can be used to search a list of values for a given value, plus or minus a given tolerance
    /// The input list need not be sorted, since mPointerIndices() will be populated when the data is loaded,
    /// after which the data array will be sorted
    /// </para>
    /// <para>
    /// To prevent this behavior, and save memory by not populating mPointerIndices, set UsePointerIndexArray = False
    /// </para>
    /// </summary>
    public class SearchRange
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SearchRange()
        {
            InitializeLocalVariables();
        }

        private enum DataTypeToUse
        {
            NoDataPresent = 0,
            IntegerType = 1,
            FloatType = 2,
            DoubleType = 3
        }

        private DataTypeToUse mDataType;

        private int[] mDataInt;
        private float[] mDataSingle;
        private double[] mDataDouble;

        /// <summary>
        /// Pointers to the original index of the data point in the source array
        /// </summary>
        private int[] mPointerIndices;

        private bool mPointerArrayIsValid;

        /// <summary>
        /// Number of data points tracked by this class (stored via FillWithData)
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public int DataCount
        {
            get
            {
                return mDataType switch
                {
                    DataTypeToUse.IntegerType => mDataInt.Length,
                    DataTypeToUse.FloatType => mDataSingle.Length,
                    DataTypeToUse.DoubleType => mDataDouble.Length,
                    DataTypeToUse.NoDataPresent => 0,
                    _ => throw new Exception("Unknown data type encountered: " + mDataType)
                };
            }
        }

        /// <summary>
        /// Get the original index of a data point, given its current index
        /// </summary>
        /// <param name="index"></param>
        // ReSharper disable once UnusedMember.Global
        public int GetOriginalIndex(int index)
        {
            if (!mPointerArrayIsValid)
                return -1;

            try
            {
                if (index < mPointerIndices.Length)
                {
                    return mPointerIndices[index];
                }

                return -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// When true, keep track of the original index of each data point
        /// Set this to false to conserve memory usage
        /// </summary>
        public bool UsePointerIndexArray { get; set; }

        /// <summary>
        /// Recursively search for the given integer, +/- tolerance
        /// </summary>
        /// <param name="searchValue"></param>
        /// <param name="toleranceHalfWidth"></param>
        /// <param name="matchIndexStart"></param>
        /// <param name="matchIndexEnd"></param>
        private void BinarySearchRangeInteger(int searchValue, int toleranceHalfWidth, ref int matchIndexStart, ref int matchIndexEnd)
        {
            var leftDone = false;
            var rightDone = false;

            var indexMidpoint = (matchIndexStart + matchIndexEnd) / 2;
            if (indexMidpoint == matchIndexStart)
            {
                // Min and Max are next to each other
                if (Math.Abs(searchValue - mDataInt[matchIndexStart]) > toleranceHalfWidth)
                    matchIndexStart = matchIndexEnd;
                if (Math.Abs(searchValue - mDataInt[matchIndexEnd]) > toleranceHalfWidth)
                    matchIndexEnd = indexMidpoint;
                return;
            }

            if (mDataInt[indexMidpoint] > searchValue + toleranceHalfWidth)
            {
                // Out of range on the right
                matchIndexEnd = indexMidpoint;
                BinarySearchRangeInteger(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }
            else if (mDataInt[indexMidpoint] < searchValue - toleranceHalfWidth)
            {
                // Out of range on the left
                matchIndexStart = indexMidpoint;
                BinarySearchRangeInteger(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }
            else
            {
                // Inside range; figure out the borders
                var leftIndex = indexMidpoint;
                do
                {
                    leftIndex--;
                    if (leftIndex < matchIndexStart)
                    {
                        leftDone = true;
                    }
                    else if (Math.Abs(searchValue - mDataInt[leftIndex]) > toleranceHalfWidth)
                    {
                        leftDone = true;
                    }
                }
                while (!leftDone);

                var rightIndex = indexMidpoint;
                do
                {
                    rightIndex++;
                    if (rightIndex > matchIndexEnd)
                    {
                        rightDone = true;
                    }
                    else if (Math.Abs(searchValue - mDataInt[rightIndex]) > toleranceHalfWidth)
                    {
                        rightDone = true;
                    }
                }
                while (!rightDone);

                matchIndexStart = leftIndex + 1;
                matchIndexEnd = rightIndex - 1;
            }
        }

        /// <summary>
        /// Recursively search for the given float, +/- tolerance
        /// </summary>
        /// <param name="searchValue"></param>
        /// <param name="toleranceHalfWidth"></param>
        /// <param name="matchIndexStart"></param>
        /// <param name="matchIndexEnd"></param>
        private void BinarySearchRangeFloat(float searchValue, float toleranceHalfWidth, ref int matchIndexStart, ref int matchIndexEnd)
        {
            var leftDone = false;
            var rightDone = false;

            var indexMidpoint = (matchIndexStart + matchIndexEnd) / 2;
            if (indexMidpoint == matchIndexStart)
            {
                // Min and Max are next to each other
                if (Math.Abs(searchValue - mDataSingle[matchIndexStart]) > toleranceHalfWidth)
                    matchIndexStart = matchIndexEnd;
                if (Math.Abs(searchValue - mDataSingle[matchIndexEnd]) > toleranceHalfWidth)
                    matchIndexEnd = indexMidpoint;
                return;
            }

            if (mDataSingle[indexMidpoint] > searchValue + toleranceHalfWidth)
            {
                // Out of range on the right
                matchIndexEnd = indexMidpoint;
                BinarySearchRangeFloat(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }
            else if (mDataSingle[indexMidpoint] < searchValue - toleranceHalfWidth)
            {
                // Out of range on the left
                matchIndexStart = indexMidpoint;
                BinarySearchRangeFloat(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }
            else
            {
                // Inside range; figure out the borders
                var leftIndex = indexMidpoint;
                do
                {
                    leftIndex--;
                    if (leftIndex < matchIndexStart)
                    {
                        leftDone = true;
                    }
                    else if (Math.Abs(searchValue - mDataSingle[leftIndex]) > toleranceHalfWidth)
                    {
                        leftDone = true;
                    }
                }
                while (!leftDone);

                var rightIndex = indexMidpoint;
                do
                {
                    rightIndex++;
                    if (rightIndex > matchIndexEnd)
                    {
                        rightDone = true;
                    }
                    else if (Math.Abs(searchValue - mDataSingle[rightIndex]) > toleranceHalfWidth)
                    {
                        rightDone = true;
                    }
                }
                while (!rightDone);

                matchIndexStart = leftIndex + 1;
                matchIndexEnd = rightIndex - 1;
            }
        }

        /// <summary>
        /// Recursively search for the given double, +/- tolerance
        /// </summary>
        /// <param name="searchValue"></param>
        /// <param name="toleranceHalfWidth"></param>
        /// <param name="matchIndexStart"></param>
        /// <param name="matchIndexEnd"></param>
        private void BinarySearchRangeDouble(double searchValue, double toleranceHalfWidth, ref int matchIndexStart, ref int matchIndexEnd)
        {
            var leftDone = false;
            var rightDone = false;

            var indexMidpoint = (matchIndexStart + matchIndexEnd) / 2;
            if (indexMidpoint == matchIndexStart)
            {
                // Min and Max are next to each other
                if (Math.Abs(searchValue - mDataDouble[matchIndexStart]) > toleranceHalfWidth)
                    matchIndexStart = matchIndexEnd;
                if (Math.Abs(searchValue - mDataDouble[matchIndexEnd]) > toleranceHalfWidth)
                    matchIndexEnd = indexMidpoint;
                return;
            }

            if (mDataDouble[indexMidpoint] > searchValue + toleranceHalfWidth)
            {
                // Out of range on the right
                matchIndexEnd = indexMidpoint;
                BinarySearchRangeDouble(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }
            else if (mDataDouble[indexMidpoint] < searchValue - toleranceHalfWidth)
            {
                // Out of range on the left
                matchIndexStart = indexMidpoint;
                BinarySearchRangeDouble(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }
            else
            {
                // Inside range; figure out the borders
                var leftIndex = indexMidpoint;
                do
                {
                    leftIndex--;
                    if (leftIndex < matchIndexStart)
                    {
                        leftDone = true;
                    }
                    else if (Math.Abs(searchValue - mDataDouble[leftIndex]) > toleranceHalfWidth)
                    {
                        leftDone = true;
                    }
                }
                while (!leftDone);

                var rightIndex = indexMidpoint;
                do
                {
                    rightIndex++;
                    if (rightIndex > matchIndexEnd)
                    {
                        rightDone = true;
                    }
                    else if (Math.Abs(searchValue - mDataDouble[rightIndex]) > toleranceHalfWidth)
                    {
                        rightDone = true;
                    }
                }
                while (!rightDone);

                matchIndexStart = leftIndex + 1;
                matchIndexEnd = rightIndex - 1;
            }
        }

        private void ClearUnusedData()
        {
            if (mDataType != DataTypeToUse.IntegerType)
                mDataInt = Array.Empty<int>();

            if (mDataType != DataTypeToUse.FloatType)
                mDataSingle = Array.Empty<float>();

            if (mDataType != DataTypeToUse.DoubleType)
                mDataDouble = Array.Empty<double>();

            if (mDataType == DataTypeToUse.NoDataPresent)
            {
                mPointerArrayIsValid = false;
            }
        }

        /// <summary>
        /// Clear stored data
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void ClearData()
        {
            mDataType = DataTypeToUse.NoDataPresent;
            ClearUnusedData();
        }

        /// <summary>
        /// Store data to search (integers)
        /// The data is sorted after being stored
        /// </summary>
        /// <remarks>This class can only track one set of data at a time (doubles, floats, or integers)</remarks>
        /// <param name="values"></param>
        /// <returns>True if success, false if an error</returns>
        // ReSharper disable once UnusedMember.Global
        public bool FillWithData(int[] values)
        {
            bool success;
            try
            {
                if (values == null || values.Length == 0)
                {
                    success = false;
                }
                else
                {
                    mDataInt = new int[values.Length];
                    values.CopyTo(mDataInt, 0);

                    if (UsePointerIndexArray)
                    {
                        InitializePointerIndexArray(mDataInt.Length);
                        Array.Sort(mDataInt, mPointerIndices);
                    }
                    else
                    {
                        Array.Sort(mDataInt);
                        mPointerArrayIsValid = false;
                    }

                    mDataType = DataTypeToUse.IntegerType;
                    success = true;
                }
            }
            catch (Exception)
            {
                success = false;
            }

            if (success)
                ClearUnusedData();
            return success;
        }

        /// <summary>
        /// Store data to search (floats)
        /// The data is sorted after being stored
        /// </summary>
        /// <remarks>This class can only track one set of data at a time (doubles, floats, or integers)</remarks>
        /// <param name="values"></param>
        /// <returns>True if success, false if an error</returns>
        // ReSharper disable once UnusedMember.Global
        public bool FillWithData(float[] values)
        {
            bool success;
            try
            {
                if (values == null || values.Length == 0)
                {
                    success = false;
                }
                else
                {
                    mDataSingle = new float[values.Length];
                    values.CopyTo(mDataSingle, 0);

                    if (UsePointerIndexArray)
                    {
                        InitializePointerIndexArray(mDataSingle.Length);
                        Array.Sort(mDataSingle, mPointerIndices);
                    }
                    else
                    {
                        Array.Sort(mDataSingle);
                        mPointerArrayIsValid = false;
                    }

                    mDataType = DataTypeToUse.FloatType;
                    success = true;
                }
            }
            catch (Exception)
            {
                success = false;
            }

            if (success)
                ClearUnusedData();
            return success;
        }

        /// <summary>
        /// Store data to search (doubles)
        /// The data is sorted after being stored
        /// </summary>
        /// <remarks>This class can only track one set of data at a time (doubles, floats, or integers)</remarks>
        /// <param name="values"></param>
        /// <returns>True if success, false if an error</returns>
        public bool FillWithData(double[] values)
        {
            bool success;
            try
            {
                if (values == null || values.Length == 0)
                {
                    success = false;
                }
                else
                {
                    mDataDouble = new double[values.Length];
                    values.CopyTo(mDataDouble, 0);

                    if (UsePointerIndexArray)
                    {
                        InitializePointerIndexArray(mDataDouble.Length);
                        Array.Sort(mDataDouble, mPointerIndices);
                    }
                    else
                    {
                        Array.Sort(mDataDouble);
                        mPointerArrayIsValid = false;
                    }

                    mDataType = DataTypeToUse.DoubleType;
                    success = true;
                }
            }
            catch (Exception)
            {
                success = false;
            }

            if (success)
                ClearUnusedData();

            return success;
        }

        /// <summary>
        /// Searches the loaded data for searchValue with a tolerance of +/-toleranceHalfWidth
        /// Call FillWithData() prior to using this method
        /// </summary>
        /// <param name="searchValue"></param>
        /// <param name="toleranceHalfWidth"></param>
        /// <param name="matchIndexStart">Output: starting index of the data that matches the target value, within the given tolerance</param>
        /// <param name="matchIndexEnd">Output: ending index of the data that matches the target value, within the given tolerance</param>
        /// <returns>True if a match is found, otherwise false</returns>
        public bool FindValueRange(int searchValue, int toleranceHalfWidth, out int matchIndexStart, out int matchIndexEnd)
        {
            bool matchFound;
            matchIndexStart = -1;
            matchIndexEnd = -1;

            if (mDataType != DataTypeToUse.IntegerType)
            {
                matchFound = mDataType switch
                {
                    DataTypeToUse.FloatType => FindValueRange((float)searchValue, toleranceHalfWidth, out matchIndexStart, out matchIndexEnd),
                    DataTypeToUse.DoubleType => FindValueRange((double)searchValue, toleranceHalfWidth, out matchIndexStart, out matchIndexEnd),
                    _ => false
                };
            }
            else
            {
                matchIndexStart = 0;
                matchIndexEnd = mDataInt.Length - 1;

                if (mDataInt.Length == 0)
                {
                    matchIndexEnd = -1;
                }
                else if (mDataInt.Length == 1)
                {
                    if (Math.Abs(searchValue - mDataInt[0]) > toleranceHalfWidth)
                    {
                        // Only one data point, and it is not within tolerance
                        matchIndexEnd = -1;
                    }
                }
                else
                {
                    BinarySearchRangeInteger(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
                }

                if (matchIndexStart > matchIndexEnd)
                {
                    matchIndexStart = -1;
                    matchIndexEnd = -1;
                    matchFound = false;
                }
                else
                {
                    matchFound = true;
                }
            }

            return matchFound;
        }

        /// <summary>
        /// Searches the loaded data for searchValue with a tolerance of +/-toleranceHalfWidth
        /// Call FillWithData() prior to using this method
        /// </summary>
        /// <param name="searchValue"></param>
        /// <param name="toleranceHalfWidth"></param>
        /// <param name="matchIndexStart">Output: starting index of the data that matches the target value, within the given tolerance</param>
        /// <param name="matchIndexEnd">Output: ending index of the data that matches the target value, within the given tolerance</param>
        /// <returns>True if a match is found, otherwise false</returns>
        public bool FindValueRange(double searchValue, double toleranceHalfWidth, out int matchIndexStart, out int matchIndexEnd)
        {
            bool matchFound;
            matchIndexStart = -1;
            matchIndexEnd = -1;

            if (mDataType != DataTypeToUse.DoubleType)
            {
                matchFound = mDataType switch
                {
                    DataTypeToUse.IntegerType => FindValueRange((int)Math.Round(searchValue), (int)Math.Round(toleranceHalfWidth),
                        out matchIndexStart, out matchIndexEnd),
                    DataTypeToUse.FloatType => FindValueRange((float)searchValue, (float)toleranceHalfWidth, out matchIndexStart, out matchIndexEnd),
                    _ => false
                };
            }
            else
            {
                matchIndexStart = 0;
                matchIndexEnd = mDataDouble.Length - 1;

                if (mDataDouble.Length == 0)
                {
                    matchIndexEnd = -1;
                }
                else if (mDataDouble.Length == 1)
                {
                    if (Math.Abs(searchValue - mDataDouble[0]) > toleranceHalfWidth)
                    {
                        // Only one data point, and it is not within tolerance
                        matchIndexEnd = -1;
                    }
                }
                else
                {
                    BinarySearchRangeDouble(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
                }

                if (matchIndexStart > matchIndexEnd)
                {
                    matchIndexStart = -1;
                    matchIndexEnd = -1;
                    matchFound = false;
                }
                else
                {
                    matchFound = true;
                }
            }

            return matchFound;
        }

        /// <summary>
        /// Searches the loaded data for searchValue with a tolerance of +/-toleranceHalfWidth
        /// Call FillWithData() prior to using this method
        /// </summary>
        /// <param name="searchValue"></param>
        /// <param name="toleranceHalfWidth"></param>
        /// <param name="matchIndexStart">Output: starting index of the data that matches the target value, within the given tolerance</param>
        /// <param name="matchIndexEnd">Output: ending index of the data that matches the target value, within the given tolerance</param>
        /// <returns>True if a match is found, otherwise false</returns>
        public bool FindValueRange(float searchValue, float toleranceHalfWidth, out int matchIndexStart, out int matchIndexEnd)
        {
            bool matchFound;
            matchIndexStart = -1;
            matchIndexEnd = -1;

            if (mDataType != DataTypeToUse.FloatType)
            {
                matchFound = mDataType switch
                {
                    DataTypeToUse.IntegerType => FindValueRange((int)Math.Round(searchValue), (int)Math.Round(toleranceHalfWidth),
                        out matchIndexStart, out matchIndexEnd),
                    DataTypeToUse.DoubleType => FindValueRange((double)searchValue, toleranceHalfWidth, out matchIndexStart, out matchIndexEnd),
                    _ => false
                };
            }
            else
            {
                matchIndexStart = 0;
                matchIndexEnd = mDataSingle.Length - 1;

                if (mDataSingle.Length == 0)
                {
                    matchIndexEnd = -1;
                }
                else if (mDataSingle.Length == 1)
                {
                    if (Math.Abs(searchValue - mDataSingle[0]) > toleranceHalfWidth)
                    {
                        // Only one data point, and it is not within tolerance
                        matchIndexEnd = -1;
                    }
                }
                else
                {
                    BinarySearchRangeFloat(searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
                }

                if (matchIndexStart > matchIndexEnd)
                {
                    matchIndexStart = -1;
                    matchIndexEnd = -1;
                    matchFound = false;
                }
                else
                {
                    matchFound = true;
                }
            }

            return matchFound;
        }

        /// <summary>
        /// Get the value stored at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The value, or 0 if no data, an invalid index, or an error</returns>
        // ReSharper disable once UnusedMember.Global
        public int GetValueByIndexInt(int index)
        {
            try
            {
                return (int)Math.Round(GetValueByIndex(index));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Get the value stored at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The value, or 0 if no data, an invalid index, or an error</returns>
        public double GetValueByIndex(int index)
        {
            try
            {
                if (mDataType == DataTypeToUse.NoDataPresent)
                {
                    return 0;
                }

                switch (mDataType)
                {
                    case DataTypeToUse.IntegerType:
                        return mDataInt[index];
                    case DataTypeToUse.FloatType:
                        return mDataSingle[index];
                    case DataTypeToUse.DoubleType:
                        return mDataDouble[index];
                }
            }
            catch (Exception)
            {
                // index is probably out of range
                // Ignore errors
            }

            return 0;
        }

        /// <summary>
        /// Get the value stored at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The value, or 0 if no data, an invalid index, or an error</returns>
        // ReSharper disable once UnusedMember.Global
        public float GetValueByIndexFloat(int index)
        {
            try
            {
                return (float)GetValueByIndex(index);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Get the value stored at the given original index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The value, or 0 if no data, an invalid index, or an error</returns>
        // ReSharper disable once UnusedMember.Global
        public int GetValueByOriginalIndexInt(int index)
        {
            try
            {
                return (int)Math.Round(GetValueByOriginalIndex(index));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Get the value stored at the given original index
        /// </summary>
        /// <param name="indexOriginal"></param>
        /// <returns>The value, or 0 if no data, an invalid index, or an error</returns>
        public double GetValueByOriginalIndex(int indexOriginal)
        {
            if (!mPointerArrayIsValid || mDataType == DataTypeToUse.NoDataPresent)
            {
                return 0;
            }

            try
            {
                var index = Array.IndexOf(mPointerIndices, indexOriginal);
                if (index >= 0)
                {
                    switch (mDataType)
                    {
                        case DataTypeToUse.IntegerType:
                            return mDataInt[mPointerIndices[index]];
                        case DataTypeToUse.FloatType:
                            return mDataSingle[mPointerIndices[index]];
                        case DataTypeToUse.DoubleType:
                            return mDataDouble[mPointerIndices[index]];
                    }
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                // Ignore errors
            }

            return 0;
        }

        /// <summary>
        /// Get the value stored at the given original index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The value, or 0 if no data, an invalid index, or an error</returns>
        // ReSharper disable once UnusedMember.Global
        public float GetValueByOriginalIndexFloat(int index)
        {
            try
            {
                return (float)GetValueByOriginalIndex(index);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private void InitializeLocalVariables()
        {
            mDataType = DataTypeToUse.NoDataPresent;
            ClearUnusedData();

            UsePointerIndexArray = true;
            InitializePointerIndexArray(0);
        }

        private void InitializePointerIndexArray(int arrayLength)
        {
            if (arrayLength < 0)
                arrayLength = 0;
            mPointerIndices = new int[arrayLength];

            for (var index = 0; index < arrayLength; index++)
            {
                mPointerIndices[index] = index;
            }

            mPointerArrayIsValid = arrayLength > 0;
        }
    }
}
