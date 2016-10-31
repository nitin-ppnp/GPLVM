using System.Collections.Generic;
using ILNumerics;

namespace GPLVM
{
    public class Data
    {
        private Dictionary<string, ILArray<double>> _dataset;

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// All needed data of an GPLVM object and its subobjects is stored in a dictonary.
        /// </remarks>
        public Data()
        {
            _dataset = new Dictionary<string, ILArray<double>>();
        }

        public Dictionary<string, ILArray<double>> Dataset
        {
            get
            {
                return _dataset;
            }
        }

        /// <summary>
        /// Adds data to the dictonary. 
        /// </summary>
        /// <remarks>
        /// If the string exists the function appends the data to the existing data object.
        /// </remarks>
        /// <param name="str">The dictonary string.</param>
        /// <param name="data">The data wants to be added.</param>
        public void AddData(string str, ILArray<double> data)
        {
            if (!_dataset.ContainsKey(str))
            {
                _dataset.Add(str, data);
            }
            else
            {
                _dataset[str][ILMath.r(ILMath.end + 1, ILMath.end + data.Size[0]), ILMath.full] = data;
            }
        }

        /// <summary>
        /// Sets data to the dictonary. 
        /// </summary>
        /// <remarks>
        /// If the string exists the function sets the data to the existing data object.
        /// </remarks>
        /// <param name="str">The dictonary string.</param>
        /// <param name="data">The data wants to be added.</param>
        public void SetData(string str, ILInArray<double> inData)
        {
            using (ILScope.Enter(inData))
            {
                ILArray<double> data = ILMath.check(inData);

                if (_dataset.ContainsKey(str))
                {
                    _dataset[str].a = data;
                }
                else
                {
                    System.Console.WriteLine("Dictionary Data: Data not found!");
                }
            }
        }

        /// <summary>
        /// Removes an entry in the dictonary. 
        /// </summary>
        /// <param name="str">The dictonary string.</param>
        public void RemoveData(string str)
        {
            _dataset.Remove(str);
        }

        /// <summary>
        /// Returns the requested data in the dictonary. 
        /// </summary>
        /// <param name="str">The dictonary string.</param>
        public ILRetArray<double> GetData(string str)
        {
            return _dataset[str].C;
        }
    }
}
