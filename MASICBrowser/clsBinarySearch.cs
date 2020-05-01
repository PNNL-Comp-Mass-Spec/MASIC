using System;
using System.Collections.Generic;

namespace MASICBrowser
{
    /// <summary>
    /// This class can be used to search a list of values for the value closest to the search value
    /// If an exact match is found, then the index of that result is returned
    /// If an exact match is not found, then the MissingDataMode defines which value will be returned (closest, always previous, or always next)
    /// </summary>
    /// <remarks>The search functions assume the input data is already sorted</remarks>
    public class clsBinarySearch
    {
        public enum eMissingDataModeConstants
        {
            ReturnClosestPoint = 0,
            ReturnPreviousPoint = 1,
            ReturnNextPoint = 2
        }

        /// <summary>
        /// Looks through listToSearch for itemToFind
        /// </summary>
        /// <param name="listToSearch"></param>
        /// <param name="itemToFind"></param>
        /// <param name="dataCount"></param>
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(
            IList<int> listToSearch, int itemToFind, int dataCount,
            eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            int matchIndex;

            try
            {
                if (listToSearch == null)
                    return -1;

                var indexFirst = 0;
                if (dataCount > listToSearch.Count)
                {
                    dataCount = listToSearch.Count;
                }

                var indexLast = dataCount - 1;

                var currentFirst = indexFirst;
                var currentLast = indexLast;

                if (currentFirst > currentLast)
                {
                    // Invalid indices were provided
                    matchIndex = -1;
                }
                else if (currentFirst == currentLast)
                {
                    // Search space is only one element long; simply return that element's index
                    matchIndex = currentFirst;
                }
                else
                {
                    var midIndex = currentLast / 2;            // Note: Using Integer division

                    while (currentFirst <= currentLast && listToSearch[midIndex] != itemToFind)
                    {
                        if (itemToFind < listToSearch[midIndex])
                        {
                            // Search the lower half
                            currentLast = midIndex - 1;
                        }
                        else if (itemToFind > listToSearch[midIndex])
                        {
                            // Search the upper half
                            currentFirst = midIndex + 1;
                        }

                        // Compute the new mid point
                        midIndex = (currentFirst + currentLast) / 2;
                        if (midIndex < currentFirst)
                        {
                            midIndex = currentFirst;
                            if (midIndex > currentLast)
                            {
                                midIndex = currentLast;
                            }

                            break;
                        }
                    }

                    matchIndex = -1;
                    // See if an exact match has been found
                    if (midIndex >= currentFirst && midIndex <= currentLast)
                    {
                        if (listToSearch[midIndex] == itemToFind)
                        {
                            matchIndex = midIndex;
                        }
                    }

                    if (matchIndex == -1)
                    {
                        if (eMissingDataMode == eMissingDataModeConstants.ReturnClosestPoint)
                        {
                            // No exact match; find the nearest match
                            if (listToSearch[midIndex] < itemToFind)
                            {
                                if (midIndex < indexLast)
                                {
                                    if (Math.Abs(listToSearch[midIndex] - itemToFind) <=
                                        Math.Abs(listToSearch[midIndex + 1] - itemToFind))
                                    {
                                        matchIndex = midIndex;
                                    }
                                    else
                                    {
                                        matchIndex = midIndex + 1;
                                    }
                                }
                                else
                                {
                                    matchIndex = indexLast;
                                }
                            }
                            // listToSearch(midIndex) >= itemToFind
                            else if (midIndex > indexFirst)
                            {
                                if (Math.Abs(listToSearch[midIndex - 1] - itemToFind) <=
                                    Math.Abs(listToSearch[midIndex] - itemToFind))
                                {
                                    matchIndex = midIndex - 1;
                                }
                                else
                                {
                                    matchIndex = midIndex;
                                }
                            }
                            else
                            {
                                matchIndex = indexFirst;
                            }
                        }
                        // No exact match; return the previous point or the next point
                        else if (listToSearch[midIndex] < itemToFind)
                        {
                            if (eMissingDataMode == eMissingDataModeConstants.ReturnNextPoint)
                            {
                                matchIndex = midIndex + 1;
                                if (matchIndex > indexLast)
                                    matchIndex = indexLast;
                            }
                            else
                            {
                                matchIndex = midIndex;
                            }
                        }
                        // listToSearch(midIndex) >= itemToFind
                        else if (eMissingDataMode == eMissingDataModeConstants.ReturnNextPoint)
                        {
                            matchIndex = midIndex;
                        }
                        else
                        {
                            matchIndex = midIndex - 1;
                            if (matchIndex < indexFirst)
                                matchIndex = indexFirst;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                matchIndex = -1;
            }

            return matchIndex;
        }

        /// <summary>
        /// Looks through listToSearch for itemToFind
        /// </summary>
        /// <param name="listToSearch"></param>
        /// <param name="itemToFind"></param>
        /// <param name="dataCount"></param>
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(
            IList<float> listToSearch, float itemToFind, int dataCount,
            eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            int matchIndex;

            try
            {
                if (listToSearch == null)
                    return -1;

                var indexFirst = 0;
                if (dataCount > listToSearch.Count)
                {
                    dataCount = listToSearch.Count;
                }

                var indexLast = dataCount - 1;

                var currentFirst = indexFirst;
                var currentLast = indexLast;

                if (currentFirst > currentLast)
                {
                    // Invalid indices were provided
                    matchIndex = -1;
                }
                else if (currentFirst == currentLast)
                {
                    // Search space is only one element long; simply return that element's index
                    matchIndex = currentFirst;
                }
                else
                {
                    var midIndex = currentLast / 2;            // Note: Using Integer division

                    while (currentFirst <= currentLast && Math.Abs(listToSearch[midIndex] - itemToFind) > float.Epsilon)
                    {
                        if (itemToFind < listToSearch[midIndex])
                        {
                            // Search the lower half
                            currentLast = midIndex - 1;
                        }
                        else if (itemToFind > listToSearch[midIndex])
                        {
                            // Search the upper half
                            currentFirst = midIndex + 1;
                        }

                        // Compute the new mid point
                        midIndex = (currentFirst + currentLast) / 2;
                        if (midIndex < currentFirst)
                        {
                            midIndex = currentFirst;
                            if (midIndex > currentLast)
                            {
                                midIndex = currentLast;
                            }

                            break;
                        }
                    }

                    matchIndex = -1;
                    // See if an exact match has been found
                    if (midIndex >= currentFirst && midIndex <= currentLast)
                    {
                        if (Math.Abs(listToSearch[midIndex] - itemToFind) < float.Epsilon)
                        {
                            matchIndex = midIndex;
                        }
                    }

                    if (matchIndex == -1)
                    {
                        if (eMissingDataMode == eMissingDataModeConstants.ReturnClosestPoint)
                        {
                            // No exact match; find the nearest match
                            if (listToSearch[midIndex] < itemToFind)
                            {
                                if (midIndex < indexLast)
                                {
                                    if (Math.Abs(listToSearch[midIndex] - itemToFind) <=
                                        Math.Abs(listToSearch[midIndex + 1] - itemToFind))
                                    {
                                        matchIndex = midIndex;
                                    }
                                    else
                                    {
                                        matchIndex = midIndex + 1;
                                    }
                                }
                                else
                                {
                                    matchIndex = indexLast;
                                }
                            }
                            // listToSearch(midIndex) >= itemToFind
                            else if (midIndex > indexFirst)
                            {
                                if (Math.Abs(listToSearch[midIndex - 1] - itemToFind) <=
                                    Math.Abs(listToSearch[midIndex] - itemToFind))
                                {
                                    matchIndex = midIndex - 1;
                                }
                                else
                                {
                                    matchIndex = midIndex;
                                }
                            }
                            else
                            {
                                matchIndex = indexFirst;
                            }
                        }
                        // No exact match; return the previous point or the next point
                        else if (listToSearch[midIndex] < itemToFind)
                        {
                            if (eMissingDataMode == eMissingDataModeConstants.ReturnNextPoint)
                            {
                                matchIndex = midIndex + 1;
                                if (matchIndex > indexLast)
                                    matchIndex = indexLast;
                            }
                            else
                            {
                                matchIndex = midIndex;
                            }
                        }
                        // listToSearch(midIndex) >= itemToFind
                        else if (eMissingDataMode == eMissingDataModeConstants.ReturnNextPoint)
                        {
                            matchIndex = midIndex;
                        }
                        else
                        {
                            matchIndex = midIndex - 1;
                            if (matchIndex < indexFirst)
                                matchIndex = indexFirst;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                matchIndex = -1;
            }

            return matchIndex;
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// Looks through listToSearch for itemToFind
        /// </summary>
        /// <param name="listToSearch"></param>
        /// <param name="itemToFind"></param>
        /// <param name="dataCount"></param>
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(
            IList<double> listToSearch, double itemToFind, int dataCount,
            eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            int matchIndex;

            try
            {
                if (listToSearch == null)
                    return -1;

                var indexFirst = 0;
                if (dataCount > listToSearch.Count)
                {
                    dataCount = listToSearch.Count;
                }

                var indexLast = dataCount - 1;

                var currentFirst = indexFirst;
                var currentLast = indexLast;

                if (currentFirst > currentLast)
                {
                    // Invalid indices were provided
                    matchIndex = -1;
                }
                else if (currentFirst == currentLast)
                {
                    // Search space is only one element long; simply return that element's index
                    matchIndex = currentFirst;
                }
                else
                {
                    var midIndex = currentLast / 2;            // Note: Using Integer division

                    while (currentFirst <= currentLast && Math.Abs(listToSearch[midIndex] - itemToFind) > float.Epsilon)
                    {
                        if (itemToFind < listToSearch[midIndex])
                        {
                            // Search the lower half
                            currentLast = midIndex - 1;
                        }
                        else if (itemToFind > listToSearch[midIndex])
                        {
                            // Search the upper half
                            currentFirst = midIndex + 1;
                        }

                        // Compute the new mid point
                        midIndex = (currentFirst + currentLast) / 2;
                        if (midIndex < currentFirst)
                        {
                            midIndex = currentFirst;
                            if (midIndex > currentLast)
                            {
                                midIndex = currentLast;
                            }

                            break;
                        }
                    }

                    matchIndex = -1;
                    // See if an exact match has been found
                    if (midIndex >= currentFirst && midIndex <= currentLast)
                    {
                        if (Math.Abs(listToSearch[midIndex] - itemToFind) < double.Epsilon)
                        {
                            matchIndex = midIndex;
                        }
                    }

                    if (matchIndex == -1)
                    {
                        if (eMissingDataMode == eMissingDataModeConstants.ReturnClosestPoint)
                        {
                            // No exact match; find the nearest match
                            if (listToSearch[midIndex] < itemToFind)
                            {
                                if (midIndex < indexLast)
                                {
                                    if (Math.Abs(listToSearch[midIndex] - itemToFind) <=
                                        Math.Abs(listToSearch[midIndex + 1] - itemToFind))
                                    {
                                        matchIndex = midIndex;
                                    }
                                    else
                                    {
                                        matchIndex = midIndex + 1;
                                    }
                                }
                                else
                                {
                                    matchIndex = indexLast;
                                }
                            }
                            // listToSearch(midIndex) >= itemToFind
                            else if (midIndex > indexFirst)
                            {
                                if (Math.Abs(listToSearch[midIndex - 1] - itemToFind) <=
                                    Math.Abs(listToSearch[midIndex] - itemToFind))
                                {
                                    matchIndex = midIndex - 1;
                                }
                                else
                                {
                                    matchIndex = midIndex;
                                }
                            }
                            else
                            {
                                matchIndex = indexFirst;
                            }
                        }
                        // No exact match; return the previous point or the next point
                        else if (listToSearch[midIndex] < itemToFind)
                        {
                            if (eMissingDataMode == eMissingDataModeConstants.ReturnNextPoint)
                            {
                                matchIndex = midIndex + 1;
                                if (matchIndex > indexLast)
                                    matchIndex = indexLast;
                            }
                            else
                            {
                                matchIndex = midIndex;
                            }
                        }
                        // listToSearch(midIndex) >= itemToFind
                        else if (eMissingDataMode == eMissingDataModeConstants.ReturnNextPoint)
                        {
                            matchIndex = midIndex;
                        }
                        else
                        {
                            matchIndex = midIndex - 1;
                            if (matchIndex < indexFirst)
                                matchIndex = indexFirst;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                matchIndex = -1;
            }

            return matchIndex;
        }
    }
}
