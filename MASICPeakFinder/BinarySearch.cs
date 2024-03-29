﻿using System;
using System.Collections.Generic;

namespace MASICPeakFinder
{
    /// <summary>
    /// This class can be used to search a list of values for the value closest to the search value
    /// If an exact match is found, the index of that result is returned
    /// If an exact match is not found, the MissingDataMode defines which value will be returned (closest, always previous, or always next)
    /// </summary>
    /// <remarks>The search functions assume the input data is already sorted</remarks>
    public static class BinarySearch
    {
        /// <summary>
        /// Options for handling missing data
        /// </summary>
        public enum MissingDataModeConstants
        {
            /// <summary>
            /// Return the closest point
            /// </summary>
            ReturnClosestPoint = 0,

            /// <summary>
            /// Return the previous point
            /// </summary>
            ReturnPreviousPoint = 1,

            /// <summary>
            /// Return the next point
            /// </summary>
            ReturnNextPoint = 2
        }

        /// <summary>
        /// Looks through listToSearch for itemToFind
        /// </summary>
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        /// <param name="listToSearch"></param>
        /// <param name="itemToFind"></param>
        /// <param name="missingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on missingDataMode</returns>
        public static int BinarySearchFindNearest(
            List<int> listToSearch,
            int itemToFind,
            MissingDataModeConstants missingDataMode)
        {
            if (listToSearch == null || listToSearch.Count == 0)
                return -1;

            // Search space is only one element long; simply return that element's index
            if (listToSearch.Count == 1)
                return 0;

            var index = listToSearch.BinarySearch(itemToFind);

            // Item found
            if (index >= 0)
                return index;

            // Get the bitwise complement, it is the "insert index" (points to the next greater item)
            index = ~index;

            // The first item is the closest match
            if (index == 0)
                return 0;

            // The last item is the closest match
            if (index == listToSearch.Count)
                return index - 1;

            switch (missingDataMode)
            {
                case MissingDataModeConstants.ReturnNextPoint:
                    return index;

                case MissingDataModeConstants.ReturnPreviousPoint:
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
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        /// <param name="listToSearch"></param>
        /// <param name="itemToFind"></param>
        /// <param name="missingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on missingDataMode</returns>
        public static int BinarySearchFindNearest(
            List<float> listToSearch,
            float itemToFind,
            MissingDataModeConstants missingDataMode)
        {
            if (listToSearch == null || listToSearch.Count == 0)
                return -1;

            // Search space is only one element long; simply return that element's index
            if (listToSearch.Count == 1)
                return 0;

            var index = listToSearch.BinarySearch(itemToFind);

            // Item found
            if (index >= 0)
                return index;

            // Get the bitwise complement, it is the "insert index" (points to the next greater item)
            index = ~index;

            // The first item is the closest match
            if (index == 0)
                return 0;

            // The last item is the closest match
            if (index == listToSearch.Count)
                return index - 1;

            switch (missingDataMode)
            {
                case MissingDataModeConstants.ReturnNextPoint:
                    return index;

                case MissingDataModeConstants.ReturnPreviousPoint:
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
        /// <remarks>Assumes listToSearch is already sorted</remarks>
        /// <param name="listToSearch"></param>
        /// <param name="itemToFind"></param>
        /// <param name="missingDataMode"></param>
        /// <returns>The index of the item if found, otherwise, the index of the closest match, based on missingDataMode</returns>
        public static int BinarySearchFindNearest(
            List<double> listToSearch,
            double itemToFind,
            MissingDataModeConstants missingDataMode)
        {
            if (listToSearch == null || listToSearch.Count == 0)
                return -1;

            // Search space is only one element long; simply return that element's index
            if (listToSearch.Count == 1)
                return 0;

            var index = listToSearch.BinarySearch(itemToFind);

            // Item found
            if (index >= 0)
                return index;

            // Get the bitwise complement, it is the "insert index" (points to the next greater item)
            index = ~index;

            // The first item is the closest match
            if (index == 0)
                return 0;

            // The last item is the closest match
            if (index == listToSearch.Count)
                return index - 1;

            switch (missingDataMode)
            {
                case MissingDataModeConstants.ReturnNextPoint:
                    return index;

                case MissingDataModeConstants.ReturnPreviousPoint:
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
