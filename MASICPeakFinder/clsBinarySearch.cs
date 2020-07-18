using System;
using System.Collections.Generic;

namespace MASICPeakFinder
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
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(
            List<int> listToSearch, int itemToFind,
            eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            if (listToSearch == null || listToSearch.Count == 0)
                return -1;

            // Search space is only one element long; simply return that element's index
            if (listToSearch.Count == 1)
                return 0;

            var index = listToSearch.BinarySearch(itemToFind);

            // item found
            if (index >= 0)
                return index;

            // Get the bitwise complement, it is the "insert index" (points to the next greater item)
            index = ~index;

            // the first item is the closest match
            if (index == 0)
                return 0;

            // the last item is the closest match
            if (index == listToSearch.Count)
                return index - 1;

            switch (eMissingDataMode)
            {
                case eMissingDataModeConstants.ReturnNextPoint:
                    return index;

                case eMissingDataModeConstants.ReturnPreviousPoint:
                    return index - 1;

                default:
                    // Includes eMissingDataModeConstants.ReturnClosestPoint

                    if (Math.Abs(listToSearch[index - 1] - itemToFind) <=
                        Math.Abs(listToSearch[index] - itemToFind))
                    {
                        return index - 1;
                    }

                    return index;
            }
        }

        /// <summary>
        /// Looks through listToSearch for itemToFind
        /// </summary>
        /// <param name="listToSearch"></param>
        /// <param name="itemToFind"></param>
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(
            List<float> listToSearch, float itemToFind,
            eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            if (listToSearch == null || listToSearch.Count == 0)
                return -1;

            // Search space is only one element long; simply return that element's index
            if (listToSearch.Count == 1)
                return 0;

            var index = listToSearch.BinarySearch(itemToFind);

            // item found
            if (index >= 0)
                return index;

            // Get the bitwise complement, it is the "insert index" (points to the next greater item)
            index = ~index;

            // the first item is the closest match
            if (index == 0)
                return 0;

            // the last item is the closest match
            if (index == listToSearch.Count)
                return index - 1;

            switch (eMissingDataMode)
            {
                case eMissingDataModeConstants.ReturnNextPoint:
                    return index;

                case eMissingDataModeConstants.ReturnPreviousPoint:
                    return index - 1;

                default:
                    // Includes eMissingDataModeConstants.ReturnClosestPoint:
                    if (Math.Abs(listToSearch[index - 1] - itemToFind) <=
                        Math.Abs(listToSearch[index] - itemToFind))
                    {
                        return index - 1;
                    }

                    return index;
            }
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// Looks through listToSearch for itemToFind
        /// </summary>
        /// <param name="listToSearch"></param>
        /// <param name="itemToFind"></param>
        /// <param name="eMissingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on eMissingDataMode</returns>
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        public static int BinarySearchFindNearest(
            List<double> listToSearch, double itemToFind,
            eMissingDataModeConstants eMissingDataMode = eMissingDataModeConstants.ReturnClosestPoint)
        {
            if (listToSearch == null || listToSearch.Count == 0)
                return -1;

            // Search space is only one element long; simply return that element's index
            if (listToSearch.Count == 1)
                return 0;

            var index = listToSearch.BinarySearch(itemToFind);

            // item found
            if (index >= 0)
                return index;

            // Get the bitwise complement, it is the "insert index" (points to the next greater item)
            index = ~index;

            // the first item is the closest match
            if (index == 0)
                return 0;

            // the last item is the closest match
            if (index == listToSearch.Count)
                return index - 1;

            switch (eMissingDataMode)
            {
                case eMissingDataModeConstants.ReturnNextPoint:
                    return index;

                case eMissingDataModeConstants.ReturnPreviousPoint:
                    return index - 1;

                default:
                    // Includes eMissingDataModeConstants.ReturnClosestPoint:
                    if (Math.Abs(listToSearch[index - 1] - itemToFind) <=
                        Math.Abs(listToSearch[index] - itemToFind))
                    {
                        return index - 1;
                    }

                    return index;
            }
        }
    }
}
