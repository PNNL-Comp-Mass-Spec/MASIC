/// <summary>
/// This class can be used to search a list of values for the value closest to the search value
/// If an exact match is found, then the index of that result is returned
/// If an exact match is not found, then the MissingDataMode defines which value will be returned (closest, always previous, or always next)
/// </summary>
/// <remarks>The search functions assume the input data is already sorted</remarks>
using System;
using System.Collections.Generic;

namespace MASIC
{
    public class clsBinarySearch
    {
        public enum eMissingDataModeConstants
        {
            ReturnClosestPoint = 0,
            ReturnPreviousPoint = 1,
            ReturnNextPoint = 2
        }

        /// <summary>
        /// Looks through arrayToSearch for itemToSearchFor
        /// </summary>
        /// <param name="arrayToSearch"></param>
        /// <param name="itemToSearchFor"></param>
        /// <param name="dataCount"></param>
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes arrayToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(IList<int> arrayToSearch, int itemToSearchFor, int dataCount, eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            int indexFirst;
            int indexLast;
            int midIndex;
            int currentFirst;
            int currentLast;
            int matchIndex;
            try
            {
                if (arrayToSearch == null)
                    return -1;
                indexFirst = 0;
                if (dataCount > arrayToSearch.Count)
                {
                    dataCount = arrayToSearch.Count;
                }

                indexLast = dataCount - 1;
                currentFirst = indexFirst;
                currentLast = indexLast;
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
                    midIndex = (currentFirst + currentLast) / 2;            // Note: Using Integer division
                    if (midIndex < currentFirst)
                        midIndex = currentFirst;
                    while (currentFirst <= currentLast && arrayToSearch[midIndex] != itemToSearchFor)
                    {
                        if (itemToSearchFor < arrayToSearch[midIndex])
                        {
                            // Search the lower half
                            currentLast = midIndex - 1;
                        }
                        else if (itemToSearchFor > arrayToSearch[midIndex])
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
                        if (arrayToSearch[midIndex] == itemToSearchFor)
                        {
                            matchIndex = midIndex;
                        }
                    }

                    if (matchIndex == -1)
                    {
                        if (eMissingDataMode == eMissingDataModeConstants.ReturnClosestPoint)
                        {
                            // No exact match; find the nearest match
                            if (arrayToSearch[midIndex] < itemToSearchFor)
                            {
                                if (midIndex < indexLast)
                                {
                                    if (Math.Abs(arrayToSearch[midIndex] - itemToSearchFor) <= Math.Abs(arrayToSearch[midIndex + 1] - itemToSearchFor))
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
                            // ArrayToSearch(midIndex) >= ItemToSearchFor
                            else if (midIndex > indexFirst)
                            {
                                if (Math.Abs(arrayToSearch[midIndex - 1] - itemToSearchFor) <= Math.Abs(arrayToSearch[midIndex] - itemToSearchFor))
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
                        else if (arrayToSearch[midIndex] < itemToSearchFor)
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
                        // ArrayToSearch(midIndex) >= ItemToSearchFor
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
        /// Looks through arrayToSearch for itemToSearchFor
        /// </summary>
        /// <param name="arrayToSearch"></param>
        /// <param name="itemToSearchFor"></param>
        /// <param name="dataCount"></param>
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes arrayToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(IList<float> arrayToSearch, float itemToSearchFor, int dataCount, eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            int indexFirst;
            int indexLast;
            int midIndex;
            int currentFirst;
            int currentLast;
            int matchIndex;
            try
            {
                if (arrayToSearch == null)
                    return -1;
                indexFirst = 0;
                if (dataCount > arrayToSearch.Count)
                {
                    dataCount = arrayToSearch.Count;
                }

                indexLast = dataCount - 1;
                currentFirst = indexFirst;
                currentLast = indexLast;
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
                    midIndex = (currentFirst + currentLast) / 2;            // Note: Using Integer division
                    if (midIndex < currentFirst)
                        midIndex = currentFirst;
                    while (currentFirst <= currentLast && Math.Abs(arrayToSearch[midIndex] - itemToSearchFor) > float.Epsilon)
                    {
                        if (itemToSearchFor < arrayToSearch[midIndex])
                        {
                            // Search the lower half
                            currentLast = midIndex - 1;
                        }
                        else if (itemToSearchFor > arrayToSearch[midIndex])
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
                        if (Math.Abs(arrayToSearch[midIndex] - itemToSearchFor) < float.Epsilon)
                        {
                            matchIndex = midIndex;
                        }
                    }

                    if (matchIndex == -1)
                    {
                        if (eMissingDataMode == eMissingDataModeConstants.ReturnClosestPoint)
                        {
                            // No exact match; find the nearest match
                            if (arrayToSearch[midIndex] < itemToSearchFor)
                            {
                                if (midIndex < indexLast)
                                {
                                    if (Math.Abs(arrayToSearch[midIndex] - itemToSearchFor) <= Math.Abs(arrayToSearch[midIndex + 1] - itemToSearchFor))
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
                            // ArrayToSearch(midIndex) >= ItemToSearchFor
                            else if (midIndex > indexFirst)
                            {
                                if (Math.Abs(arrayToSearch[midIndex - 1] - itemToSearchFor) <= Math.Abs(arrayToSearch[midIndex] - itemToSearchFor))
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
                        else if (arrayToSearch[midIndex] < itemToSearchFor)
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
                        // ArrayToSearch(midIndex) >= ItemToSearchFor
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
        /// Looks through arrayToSearch for itemToSearchFor
        /// </summary>
        /// <param name="arrayToSearch"></param>
        /// <param name="itemToSearchFor"></param>
        /// <param name="dataCount"></param>
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes arrayToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(IList<double> arrayToSearch, double itemToSearchFor, int dataCount, eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            int indexFirst;
            int indexLast;
            int midIndex;
            int currentFirst;
            int currentLast;
            int matchIndex;
            try
            {
                if (arrayToSearch == null)
                    return -1;
                indexFirst = 0;
                if (dataCount > arrayToSearch.Count)
                {
                    dataCount = arrayToSearch.Count;
                }

                indexLast = dataCount - 1;
                currentFirst = indexFirst;
                currentLast = indexLast;
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
                    midIndex = (currentFirst + currentLast) / 2;            // Note: Using Integer division
                    if (midIndex < currentFirst)
                        midIndex = currentFirst;
                    while (currentFirst <= currentLast && Math.Abs(arrayToSearch[midIndex] - itemToSearchFor) > float.Epsilon)
                    {
                        if (itemToSearchFor < arrayToSearch[midIndex])
                        {
                            // Search the lower half
                            currentLast = midIndex - 1;
                        }
                        else if (itemToSearchFor > arrayToSearch[midIndex])
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
                        if (Math.Abs(arrayToSearch[midIndex] - itemToSearchFor) < double.Epsilon)
                        {
                            matchIndex = midIndex;
                        }
                    }

                    if (matchIndex == -1)
                    {
                        if (eMissingDataMode == eMissingDataModeConstants.ReturnClosestPoint)
                        {
                            // No exact match; find the nearest match
                            if (arrayToSearch[midIndex] < itemToSearchFor)
                            {
                                if (midIndex < indexLast)
                                {
                                    if (Math.Abs(arrayToSearch[midIndex] - itemToSearchFor) <= Math.Abs(arrayToSearch[midIndex + 1] - itemToSearchFor))
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
                            // ArrayToSearch(midIndex) >= ItemToSearchFor
                            else if (midIndex > indexFirst)
                            {
                                if (Math.Abs(arrayToSearch[midIndex - 1] - itemToSearchFor) <= Math.Abs(arrayToSearch[midIndex] - itemToSearchFor))
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
                        else if (arrayToSearch[midIndex] < itemToSearchFor)
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
                        // ArrayToSearch(midIndex) >= ItemToSearchFor
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