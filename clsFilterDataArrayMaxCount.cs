using System;

namespace MASIC
{
    public class clsFilterDataArrayMaxCount
    {
        // This class can be used to select the top N data points in a list, sorting descending
        // It does not require a full sort of the data, which allows for faster filtering of the data
        //
        // To use, first call AddDataPoint() for each source data point, specifying the value to sort on and a data point index
        // When done, call FilterData()
        // This routine will determine which data points to retain
        // For the remaining points, their data values will be changed to mSkipDataPointFlag (defaults to -1)

        private const int INITIAL_MEMORY_RESERVE = 50000;

        private const float DEFAULT_SKIP_DATA_POINT_FLAG = -1;

        // 4 steps in Sub FilterDataByMaxDataCountToLoad
        private const int SUBTASK_STEP_COUNT = 4;

        private int mDataCount;
        private double[] mDataValues = new double[0];
        private int[] mDataIndices = new int[0];

        private float mProgress;     // Value between 0 and 100

        public event ProgressChangedEventHandler ProgressChanged;

        public delegate void ProgressChangedEventHandler(float progressVal);

        #region "Properties"
        public int MaximumDataCountToLoad { get; set; }

        public float Progress => mProgress;

        public float SkipDataPointFlag { get; set; }

        public bool TotalIntensityPercentageFilterEnabled { get; set; }

        public float TotalIntensityPercentageFilter { get; set; }
        #endregion

        public clsFilterDataArrayMaxCount()
            : this(INITIAL_MEMORY_RESERVE)
        {
        }

        public clsFilterDataArrayMaxCount(int initialCapacity)
        {
            SkipDataPointFlag = DEFAULT_SKIP_DATA_POINT_FLAG;
            Clear(initialCapacity);
        }

        public void AddDataPoint(double abundance, int dataPointIndex)
        {
            if (mDataCount >= mDataValues.Length)
            {
                var oldMDataValues = mDataValues;
                mDataValues = new double[((int)Math.Floor(mDataValues.Length * 1.5))];
                Array.Copy(oldMDataValues, mDataValues, Math.Min((int)Math.Floor(mDataValues.Length * 1.5), oldMDataValues.Length));
                var oldMDataIndices = mDataIndices;
                mDataIndices = new int[mDataValues.Length];
                Array.Copy(oldMDataIndices, mDataIndices, Math.Min(mDataValues.Length, oldMDataIndices.Length));
            }

            mDataValues[mDataCount] = abundance;
            mDataIndices[mDataCount] = dataPointIndex;

            mDataCount += 1;
        }

        public void Clear(int initialCapacity)
        {
            MaximumDataCountToLoad = 400000;

            TotalIntensityPercentageFilterEnabled = false;
            TotalIntensityPercentageFilter = 90;

            if (initialCapacity < 4)
            {
                initialCapacity = 4;
            }

            mDataCount = 0;
            mDataValues = new double[initialCapacity];
            mDataIndices = new int[initialCapacity];
        }

        public double GetAbundanceByIndex(int dataPointIndex)
        {
            if (dataPointIndex >= 0 && dataPointIndex < mDataCount)
            {
                return mDataValues[dataPointIndex];
            }
            else
            {
                // Invalid data point index value
                return -1;
            }
        }

        public void FilterData()
        {
            if (mDataCount <= 0)
            {
                // Nothing to do
            }
            else
            {
                // Shrink the arrays to mDataCount
                if (mDataCount < mDataValues.Length)
                {
                    var oldMDataValues = mDataValues;
                    mDataValues = new double[mDataCount];
                    Array.Copy(oldMDataValues, mDataValues, Math.Min(mDataCount, oldMDataValues.Length));
                    var oldMDataIndices = mDataIndices;
                    mDataIndices = new int[mDataCount];
                    Array.Copy(oldMDataIndices, mDataIndices, Math.Min(mDataCount, oldMDataIndices.Length));
                }

                FilterDataByMaxDataCountToKeep();
            }
        }

        private void FilterDataByMaxDataCountToKeep()
        {
            const int HISTOGRAM_BIN_COUNT = 5000;

            try
            {
                double[] binToSortAbundances;
                int[] binToSortDataIndices;

                binToSortAbundances = new double[10];
                binToSortDataIndices = new int[10];

                UpdateProgress(0);

                var useFullDataSort = false;
                if (mDataCount == 0)
                {
                    // No data loaded
                    UpdateProgress((float)(4 / (double)SUBTASK_STEP_COUNT * 100.0D));
                    return;
                }
                else if (mDataCount <= MaximumDataCountToLoad)
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
                for (var index = 0; index <= mDataCount - 1; index++)
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

                int[] histogramBinCounts;
                double[] histogramBinStartIntensity;

                histogramBinCounts = new int[binCount];
                histogramBinStartIntensity = new double[binCount];

                for (var index = 0; index <= binCount - 1; index++)
                    histogramBinStartIntensity[index] = index * binSize;

                // Parse mDataValues to populate histogramBinCounts
                for (var index = 0; index <= mDataCount - 1; index++)
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
                            targetBin += 1;
                        }
                    }

                    histogramBinCounts[targetBin] += 1;

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
                        UpdateProgress((float)((0 + (index + 1) / (double)mDataCount) / SUBTASK_STEP_COUNT * 100.0));
                    }
                }

                // Now examine the frequencies in histogramBinCounts() to determine the minimum value to consider when sorting
                var pointTotal = 0;
                var binToSort = -1;
                for (var index = binCount - 1; index >= 0; index += -1)
                {
                    pointTotal = pointTotal + histogramBinCounts[index];
                    if (pointTotal >= MaximumDataCountToLoad)
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
                        for (var index = 0; index <= mDataCount - 1; index++)
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
                                binToSortDataCount += 1;
                            }
                            else
                            {
                                dataCountImplicitlyIncluded = dataCountImplicitlyIncluded + 1;
                            }

                            if (index % 10000 == 0)
                            {
                                UpdateProgress((float)((1 + (index + 1) / (double)mDataCount) / SUBTASK_STEP_COUNT * 100.0D));
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

                        if (MaximumDataCountToLoad - dataCountImplicitlyIncluded - binToSortDataCount == 0)
                        {
                            // No need to sort and examine the data for BinToSort since we'll ultimately include all of it
                        }
                        else
                        {
                            SortAndMarkPointsToSkip(ref binToSortAbundances, ref binToSortDataIndices, binToSortDataCount, MaximumDataCountToLoad - dataCountImplicitlyIncluded, SUBTASK_STEP_COUNT);
                        }

                        // Synchronize the data in binToSortAbundances and binToSortDataIndices with mDataValues and mDataValues
                        // mDataValues and mDataIndices have not been sorted and therefore mDataIndices should currently be sorted ascending on "valid data point index"
                        // binToSortDataIndices should also currently be sorted ascending on "valid data point index" so the following Do Loop within a For Loop should sync things up

                        var originalDataArrayIndex = 0;
                        for (var index = 0; index <= binToSortDataCount - 1; index++)
                        {
                            while (binToSortDataIndices[index] > mDataIndices[originalDataArrayIndex])
                                originalDataArrayIndex += 1;

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

                            originalDataArrayIndex += 1;

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
                    SortAndMarkPointsToSkip(ref mDataValues, ref mDataIndices, mDataCount, MaximumDataCountToLoad, SUBTASK_STEP_COUNT);
                }

                UpdateProgress((float)(4 / (double)SUBTASK_STEP_COUNT * 100.0D));

                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in FilterDataByMaxDataCountToKeep: " + ex.Message, ex);
            }
        }

        // This is sub uses a full sort to filter the data
        // This will be slow for large arrays and you should therefore use FilterDataByMaxDataCountToKeep if possible
        private void SortAndMarkPointsToSkip(ref double[] abundances, ref int[] dataIndices, int dataCount, int maximumDataCountInArraysToLoad, int subtaskStepCount)
        {
            int index;

            if (dataCount > 0)
            {
                // Sort abundances ascending, sorting dataIndices in parallel
                Array.Sort(abundances, dataIndices, 0, dataCount);

                UpdateProgress((float)(2.333 / subtaskStepCount * 100.0));

                // Change the abundance values to mSkipDataPointFlag for data up to index dataCount-maximumDataCountInArraysToLoad-1
                for (index = 0; index <= dataCount - maximumDataCountInArraysToLoad - 1; index++)
                    abundances[index] = SkipDataPointFlag;

                UpdateProgress((float)(2.666 / subtaskStepCount * 100.0D));

                // Re-sort, this time on dataIndices with abundances in parallel
                Array.Sort(dataIndices, abundances, 0, dataCount);
            }

            UpdateProgress((float)(3 / (double)subtaskStepCount * 100.0D));
        }

        private void UpdateProgress(float progressValue)
        {
            mProgress = progressValue;

            ProgressChanged?.Invoke(mProgress);
        }
    }
}
