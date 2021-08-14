using System;

namespace MASIC
{
    /// <summary>
    /// This class can be used to select the top N data points in a list, sorting descending
    /// It does not require a full sort of the data, which allows for faster filtering of the data
    /// </summary>
    /// <remarks>
    /// To use, first call AddDataPoint() for each source data point, specifying the value to sort on and a data point index
    /// When done, call FilterData()
    /// This routine will determine which data points to retain
    /// For the remaining points, their data values will be changed to SkipDataPointFlag (defaults to -1)
    /// </remarks>
    public class clsFilterDataArrayMaxCount
    {
        private const int INITIAL_MEMORY_RESERVE = 50000;

        private const float DEFAULT_SKIP_DATA_POINT_FLAG = -1;

        // 4 steps in method FilterDataByMaxDataCountToLoad
        private const int SUBTASK_STEP_COUNT = 4;

        private double[] mDataValues = new double[0];
        private int[] mDataIndices = new int[0];

        public event ProgressChangedEventHandler ProgressChanged;

        public delegate void ProgressChangedEventHandler(float progressVal);

        public int DataCount { get; private set; }

        public int MaximumDataCountToKeep { get; set; }

        /// <summary>
        /// Progress
        /// </summary>
        /// <remarks>Value between 0 and 100</remarks>
        public float Progress { get; private set; }

        public float SkipDataPointFlag { get; set; }

        public bool TotalIntensityPercentageFilterEnabled { get; set; }

        public float TotalIntensityPercentageFilter { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsFilterDataArrayMaxCount()
            : this(INITIAL_MEMORY_RESERVE)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialCapacity"></param>
        public clsFilterDataArrayMaxCount(int initialCapacity)
        {
            SkipDataPointFlag = DEFAULT_SKIP_DATA_POINT_FLAG;
            Clear(initialCapacity);
        }

        /// <summary>
        /// Add a data point
        /// </summary>
        /// <param name="abundance"></param>
        /// <param name="dataPointIndex"></param>
        public void AddDataPoint(double abundance, int dataPointIndex)
        {
            if (DataCount >= mDataValues.Length)
            {
                var oldMDataValues = mDataValues;
                mDataValues = new double[((int)Math.Floor(mDataValues.Length * 1.5))];
                Array.Copy(oldMDataValues, mDataValues, Math.Min((int)Math.Floor(mDataValues.Length * 1.5), oldMDataValues.Length));

                var oldMDataIndices = mDataIndices;
                mDataIndices = new int[mDataValues.Length];
                Array.Copy(oldMDataIndices, mDataIndices, Math.Min(mDataValues.Length, oldMDataIndices.Length));
            }

            mDataValues[DataCount] = abundance;
            mDataIndices[DataCount] = dataPointIndex;

            DataCount++;
        }

        /// <summary>
        /// Clear cached data
        /// </summary>
        /// <param name="initialCapacity"></param>
        public void Clear(int initialCapacity)
        {
            MaximumDataCountToKeep = 400000;

            TotalIntensityPercentageFilterEnabled = false;
            TotalIntensityPercentageFilter = 90;

            if (initialCapacity < 4)
            {
                initialCapacity = 4;
            }

            DataCount = 0;
            mDataValues = new double[initialCapacity];
            mDataIndices = new int[initialCapacity];
        }

        /// <summary>
        /// Get the abundance value associated with the given data point
        /// </summary>
        /// <param name="dataPointIndex"></param>
        /// <returns></returns>
        public double GetAbundanceByIndex(int dataPointIndex)
        {
            if (dataPointIndex >= 0 && dataPointIndex < DataCount)
            {
                return mDataValues[dataPointIndex];
            }

            // Invalid data point index value
            return -1;
        }

        /// <summary>
        /// Filter the stored data to assure there is no more than MaximumDataCountToKeep data points
        /// </summary>
        public void FilterData()
        {
            if (DataCount <= 0)
            {
                // Nothing to do
            }
            else
            {
                // Shrink the arrays to DataCount
                if (DataCount < mDataValues.Length)
                {
                    var oldMDataValues = mDataValues;
                    mDataValues = new double[DataCount];
                    Array.Copy(oldMDataValues, mDataValues, Math.Min(DataCount, oldMDataValues.Length));

                    var oldMDataIndices = mDataIndices;
                    mDataIndices = new int[DataCount];
                    Array.Copy(oldMDataIndices, mDataIndices, Math.Min(DataCount, oldMDataIndices.Length));
                }

                FilterDataByMaxDataCountToKeep();
            }
        }

        private void FilterDataByMaxDataCountToKeep()
        {
            const int HISTOGRAM_BIN_COUNT = 5000;

            try
            {
                var binToSortAbundances = new double[10];
                var binToSortDataIndices = new int[10];

                UpdateProgress(0);

                var useFullDataSort = false;
                if (DataCount == 0)
                {
                    // No data loaded
                    UpdateProgress((float)(4 / (double)SUBTASK_STEP_COUNT * 100.0D));
                    return;
                }

                if (DataCount <= MaximumDataCountToKeep)
                {
                    // Loaded less than mMaximumDataCountToKeep data points
                    // Nothing to filter
                    UpdateProgress((float)(4 / (double)SUBTASK_STEP_COUNT * 100.0D));
                    return;
                }

                // In order to speed up the sorting, we're first going to make a histogram
                // (aka frequency distribution) of the abundances in mDataValues

                // First, determine the maximum abundance value in mDataValues
                var maxAbundance = double.MinValue;
                for (var index = 0; index < DataCount; index++)
                {
                    if (mDataValues[index] > maxAbundance)
                    {
                        maxAbundance = mDataValues[index];
                    }
                }

                // Round maxAbundance up to the next highest integer
                maxAbundance = (long)Math.Ceiling(maxAbundance);

                // Now determine the histogram bin size
                var binSize = maxAbundance / HISTOGRAM_BIN_COUNT;
                if (binSize < 1)
                    binSize = 1;

                // Initialize histogramData
                var binCount = (int)(maxAbundance / binSize) + 1;

                var histogramBinCounts = new int[binCount];
                var histogramBinStartIntensity = new double[binCount];

                for (var index = 0; index < binCount; index++)
                {
                    histogramBinStartIntensity[index] = index * binSize;
                }

                // Parse mDataValues to populate histogramBinCounts
                for (var index = 0; index < DataCount; index++)
                {
                    int targetBin;
                    if (mDataValues[index] <= 0)
                    {
                        targetBin = 0;
                    }
                    else
                    {
                        targetBin = (int)(Math.Floor(mDataValues[index] / binSize));
                    }

                    if (targetBin < binCount - 1)
                    {
                        if (mDataValues[index] >= histogramBinStartIntensity[targetBin + 1])
                        {
                            targetBin++;
                        }
                    }

                    histogramBinCounts[targetBin]++;

                    if (mDataValues[index] < histogramBinStartIntensity[targetBin])
                    {
                        if (mDataValues[index] < histogramBinStartIntensity[targetBin] - binSize / 1000)
                        {
                            // This is unexpected
                            mDataValues[index] = mDataValues[index];
                        }
                    }

                    if (index % 10000 == 0)
                    {
                        UpdateProgress((float)((0 + (index + 1) / (double)DataCount) / SUBTASK_STEP_COUNT * 100.0));
                    }
                }

                // Now examine the frequencies in histogramBinCounts() to determine the minimum value to consider when sorting
                var pointTotal = 0;
                var binToSort = -1;
                for (var index = binCount - 1; index >= 0; index += -1)
                {
                    pointTotal = pointTotal + histogramBinCounts[index];
                    if (pointTotal >= MaximumDataCountToKeep)
                    {
                        binToSort = index;
                        break;
                    }
                }

                UpdateProgress((float)(1 / (double)SUBTASK_STEP_COUNT * 100.0D));

                if (binToSort >= 0)
                {
                    // Find the data with intensity >= histogramBinStartIntensity(binToSort)
                    // We actually only need to sort the data in bin binToSort

                    var binToSortAbundanceMinimum = histogramBinStartIntensity[binToSort];
                    var binToSortAbundanceMaximum = maxAbundance + 1;

                    if (binToSort < binCount - 1)
                    {
                        binToSortAbundanceMaximum = histogramBinStartIntensity[binToSort + 1];
                    }

                    if (Math.Abs(binToSortAbundanceMaximum - binToSortAbundanceMinimum) < float.Epsilon)
                    {
                        // Is this code ever reached?
                        // If yes, then the code below won't populate binToSortAbundances() and binToSortDataIndices() with any data
                        useFullDataSort = true;
                    }

                    var binToSortDataCount = 0;

                    if (!useFullDataSort)
                    {
                        if (histogramBinCounts[binToSort] > 0)
                        {
                            binToSortAbundances = new double[(histogramBinCounts[binToSort])];
                            binToSortDataIndices = new int[(histogramBinCounts[binToSort])];
                        }
                        else
                        {
                            // Is this code ever reached?
                            useFullDataSort = true;
                        }
                    }

                    if (!useFullDataSort)
                    {
                        var dataCountImplicitlyIncluded = 0;
                        for (var index = 0; index < DataCount; index++)
                        {
                            if (mDataValues[index] < binToSortAbundanceMinimum)
                            {
                                // Skip this data point when re-reading the input data file
                                mDataValues[index] = SkipDataPointFlag;
                            }
                            else if (mDataValues[index] < binToSortAbundanceMaximum)
                            {
                                // Value is in the bin to sort; add to the BinToSort arrays

                                if (binToSortDataCount >= binToSortAbundances.Length)
                                {
                                    // Need to reserve more space (this is unexpected)
                                    var oldBinToSortAbundances = binToSortAbundances;
                                    binToSortAbundances = new double[(binToSortAbundances.Length * 2)];
                                    Array.Copy(oldBinToSortAbundances, binToSortAbundances, Math.Min(binToSortAbundances.Length * 2, oldBinToSortAbundances.Length));
                                    var oldBinToSortDataIndices = binToSortDataIndices;
                                    binToSortDataIndices = new int[binToSortAbundances.Length];
                                    Array.Copy(oldBinToSortDataIndices, binToSortDataIndices, Math.Min(binToSortAbundances.Length, oldBinToSortDataIndices.Length));
                                }

                                binToSortAbundances[binToSortDataCount] = mDataValues[index];
                                binToSortDataIndices[binToSortDataCount] = mDataIndices[index];
                                binToSortDataCount++;
                            }
                            else
                            {
                                dataCountImplicitlyIncluded = dataCountImplicitlyIncluded + 1;
                            }

                            if (index % 10000 == 0)
                            {
                                UpdateProgress((float)((1 + (index + 1) / (double)DataCount) / SUBTASK_STEP_COUNT * 100.0D));
                            }
                        }

                        if (binToSortDataCount > 0)
                        {
                            if (binToSortDataCount < binToSortAbundances.Length)
                            {
                                var oldBinToSortAbundances1 = binToSortAbundances;
                                binToSortAbundances = new double[binToSortDataCount];
                                Array.Copy(oldBinToSortAbundances1, binToSortAbundances, Math.Min(binToSortDataCount, oldBinToSortAbundances1.Length));

                                var oldBinToSortDataIndices1 = binToSortDataIndices;
                                binToSortDataIndices = new int[binToSortDataCount];
                                Array.Copy(oldBinToSortDataIndices1, binToSortDataIndices, Math.Min(binToSortDataCount, oldBinToSortDataIndices1.Length));
                            }
                        }
                        else
                        {
                            // This code shouldn't be reached
                        }

                        if (MaximumDataCountToKeep - dataCountImplicitlyIncluded - binToSortDataCount == 0)
                        {
                            // No need to sort and examine the data for BinToSort since we'll ultimately include all of it
                        }
                        else
                        {
                            SortAndMarkPointsToSkip(binToSortAbundances, binToSortDataIndices, binToSortDataCount, MaximumDataCountToKeep - dataCountImplicitlyIncluded, SUBTASK_STEP_COUNT);
                        }

                        // Synchronize the data in binToSortAbundances and binToSortDataIndices with mDataValues and mDataValues
                        // mDataValues and mDataIndices have not been sorted and therefore mDataIndices should currently be sorted ascending on "valid data point index"
                        // binToSortDataIndices should also currently be sorted ascending on "valid data point index" so the following Do Loop within a For Loop should sync things up

                        var originalDataArrayIndex = 0;
                        for (var index = 0; index < binToSortDataCount; index++)
                        {
                            while (binToSortDataIndices[index] > mDataIndices[originalDataArrayIndex])
                            {
                                originalDataArrayIndex++;
                            }

                            if (Math.Abs(binToSortAbundances[index] - SkipDataPointFlag) < float.Epsilon)
                            {
                                if (mDataIndices[originalDataArrayIndex] == binToSortDataIndices[index])
                                {
                                    mDataValues[originalDataArrayIndex] = SkipDataPointFlag;
                                }
                                else
                                {
                                    // This code shouldn't be reached
                                }
                            }

                            originalDataArrayIndex++;

                            if (binToSortDataCount < 1000 || binToSortDataCount % 100 == 0)
                            {
                                UpdateProgress((float)((3 + (index + 1) / (double)binToSortDataCount) / SUBTASK_STEP_COUNT * 100.0));
                            }
                        }
                    }
                }
                else
                {
                    useFullDataSort = true;
                }

                if (useFullDataSort)
                {
                    // This shouldn't normally be necessary

                    // We have to sort all of the data; this can be quite slow
                    SortAndMarkPointsToSkip(mDataValues, mDataIndices, DataCount, MaximumDataCountToKeep, SUBTASK_STEP_COUNT);
                }

                UpdateProgress((float)(4 / (double)SUBTASK_STEP_COUNT * 100.0D));
            }
            catch (Exception ex)
            {
                throw new Exception("Error in FilterDataByMaxDataCountToKeep: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// This method uses a full sort to filter the data
        /// </summary>
        /// <param name="abundances"></param>
        /// <param name="dataIndices"></param>
        /// <param name="dataCount"></param>
        /// <param name="maximumDataCountInArraysToLoad"></param>
        /// <param name="subtaskStepCount"></param>
        /// <remarks>Sorting will be slow for large arrays and you should therefore use FilterDataByMaxDataCountToKeep if possible</remarks>
        private void SortAndMarkPointsToSkip(double[] abundances, int[] dataIndices, int dataCount, int maximumDataCountInArraysToLoad, int subtaskStepCount)
        {
            if (dataCount > 0)
            {
                // Sort abundances ascending, sorting dataIndices in parallel
                Array.Sort(abundances, dataIndices, 0, dataCount);

                UpdateProgress((float)(2.333 / subtaskStepCount * 100.0));

                // Change the abundance values to SkipDataPointFlag for data up to index dataCount-maximumDataCountInArraysToLoad-1
                for (var index = 0; index < dataCount - maximumDataCountInArraysToLoad; index++)
                {
                    abundances[index] = SkipDataPointFlag;
                }

                UpdateProgress((float)(2.666 / subtaskStepCount * 100.0D));

                // Re-sort, this time on dataIndices with abundances in parallel
                Array.Sort(dataIndices, abundances, 0, dataCount);
            }

            UpdateProgress((float)(3 / (double)subtaskStepCount * 100.0D));
        }

        private void UpdateProgress(float progressValue)
        {
            Progress = progressValue;

            ProgressChanged?.Invoke(Progress);
        }
    }
}
