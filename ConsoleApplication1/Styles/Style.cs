using System;
using System.Collections.Generic;
using ILNumerics;
using System.Xml;

namespace GPLVM.Styles
{
    public class Style : IStyle
    {
        private string _name;
        private List<string> _substyles;

        private ILArray<double> _style = ILMath.localMember<double>();
        private ILArray<double> _styleu = ILMath.localMember<double>();
        private ILArray<double> _factorIdx = ILMath.localMember<double>();
        private ILArray<double> _factorIdxu = ILMath.localMember<double>();

        /// <summary>
        /// The class constructor. 
        /// </summary>
        /// <remarks>
        /// Aditional latent variables in 1-of-K encoding to model characteristics of movements, e.g emotions
        /// or lokomotion characteristics (running, walking). Goal is to separate the style from the content of the motion.
        /// With the style class we want to construct a multifactor model to get and AND effect between kernel matrices and force
        /// pairs of latent points to correlate to the corresponding style. See StyleGPLVM class how the style class works.
        /// </remarks>
        /// <param name="name">The name of the style.</param>
        public Style(string name)
        {
            _name = name;
            _substyles = new List<string>();
        }

        /// <summary>
        /// Returns the number of optimaizeable parameters. 
        /// </summary>
        public int NumParameter
        {
            get
            {
                int numParam = _style.Size[0] * _style.Size[0];
                if (!_styleu.IsEmpty)
                    numParam += _style.Size[0] * _style.Size[0];
                return numParam;
            }
        }

        /// <summary>
        /// Sets and gets the parameters for optimization. 
        /// </summary>
        public ILArray<double> LogParameter
        {
            get
            {
                ILArray<double> tmp = ILMath.empty();
                tmp = _style[ILMath.full].T.C;
                if (!_styleu.IsEmpty)
                    tmp[ILMath.r(ILMath.end + 1, ILMath.end + _styleu[ILMath.full].T.Length)] = _styleu[ILMath.full].T.C;
                return tmp.C;
            }
            set
            {
                ILArray<double> param = ILMath.empty();
                param.a = value;

                int startVal = 0;
                int endVal = _substyles.Count * _substyles.Count - 1;

                _style.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _substyles.Count, _substyles.Count);

                if (!_styleu.IsEmpty)
                {
                    startVal = endVal + 1;
                    endVal += _substyles.Count * _substyles.Count;

                    _styleu.a = ILMath.reshape(param[ILMath.r(startVal, endVal)], _substyles.Count, _substyles.Count);
                }
            }
        }

        /// <summary>
        /// Returns the name of the style. 
        /// </summary>
        public string StyleName
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Returns the list of substyles. 
        /// </summary>
        public List<string> SubStyles
        {
            get
            {
                return _substyles;
            }
        }

        /// <summary>
        /// Sets and gets the latent style variable. 
        /// </summary>
        public ILArray<double> StyleVariable
        {
            get
            {
                return _style;
            }
            set
            {
                _style.a = value;
            }
        }

        /// <summary>
        /// Sets and gets the latent style inducing variable (used for DTC and FITC approximation). 
        /// </summary>
        public ILArray<double> StyleInducingVariable
        {
            get
            {
                return _styleu;
            }
            set
            {
                _styleu.a = value;
            }
        }

        /// <summary>
        /// Indexes where each style value belongs to corresponding x. 
        /// </summary>
        public ILArray<double> FactorIndex
        {
            get
            {
                return _factorIdx;
            }
        }

        /// <summary>
        /// Indexes where each style value belongs to corresponding x_u. 
        /// </summary>
        public ILArray<double> FactorIndexInducing
        {
            get
            {
                return _factorIdxu;
            }
            set
            {
                _factorIdxu.a = value.C;
                _styleu.a = _style.C;
            }
        }

        /// <summary>
        /// Computes the gradient for each style value. 
        /// </summary>
        /// <param name="gSSequence">Gradient of the style value sequence.</param>
        /// <returns>Gradient of each substyle value.</returns>
        public ILRetArray<double> StyleGradient(ILInArray<double> gSSequence)
        {
            using (ILScope.Enter(gSSequence))
            {
                ILArray<double> gSS = ILMath.check(gSSequence);
                ILArray<double> gS = ILMath.zeros(_style.Size);
                for (int i = 0; i < _substyles.Count; i++)
                    gS[i, ILMath.full] = ILMath.sum(gSS[ILMath.find(_factorIdx == i), ILMath.full],0);

                ILRetArray<double> res = gS;

                return res[ILMath.full].T;
            }
        }

        /// <summary>
        /// Computes the gradient for each style value correnponding to the inducing points. 
        /// </summary>
        /// <param name="gSSequence">Gradient of the style value sequence.</param>
        /// <returns>Gradient of each substyle value corresponding to the inducing points.</returns>
        public ILRetArray<double> StyleInducingGradient(ILInArray<double> gSSequence)
        {
            using (ILScope.Enter(gSSequence))
            {
                ILArray<double> gSS = ILMath.check(gSSequence);
                ILArray<double> gS = ILMath.zeros(_style.Size);
                for (int i = 0; i < _substyles.Count; i++)
                    gS[i, ILMath.full] = ILMath.sum(gSS[ILMath.find(_factorIdxu == i), ILMath.full], 0);

                ILRetArray<double> res = gS;

                return res[ILMath.full].T;
            }
        }

        /*public ILRetArray<double> ComputeStarMatrix(ILInArray<double> inStyleStar, int numSteps)
        {
            using (ILScope.Enter(inStyleStar))
            {
                ILArray<double> styleStar = ILMath.check(inStyleStar);

                ILArray<double> k = _kern.ComputeKernelMatrix(styleStar, _style);

                _K.a = ILMath.repmat(k[ILMath.full, _factorIdx], numSteps, 1);
                return _K;
            }
        }*/


        /// <summary>
        /// Adds a substyle value to the object if the name of the substyle doesn't exists. If it exists
        /// it adds just the indexes of the substyle with length of the added sequence.
        /// </summary>
        /// <param name="subStyleName">The name of the substyle.</param>
        /// <param name="sequenceLength">The length of the added sequence.</param>
        /// <returns>Gradient of each substyle value.</returns>
        public void AddSubStyle(string subStyleName, int seqenceLength)
        {
            if (!_substyles.Contains(subStyleName))
            {
                _style.a = ILMath.eye(_substyles.Count + 1, _substyles.Count + 1);
                _substyles.Add(subStyleName);
                if (_factorIdx.IsEmpty)
                    _factorIdx.a = ILMath.zeros(1, seqenceLength);
                else
                    _factorIdx[ILMath.r(ILMath.end + 1, ILMath.end + seqenceLength)] = _substyles.IndexOf(subStyleName);
            }
            else
            {
                int index = _substyles.IndexOf(subStyleName);
                _factorIdx[ILMath.r(ILMath.end + 1, ILMath.end + seqenceLength)] = index;
            }
        }

        public void InterpolateSubStyles(ILInArray<double> interpolationRatio)
        {

        }

        // Read and Write Data to a XML file
        public void Read(ref XmlReader reader)
        {
            string[] tokens;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.Read();
                if (reader.Name == "Data")
                {
                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.Read();
                        if (reader.Name == "Substyles")
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                //reader.Read();
                                _substyles.Add((string)reader.ReadContentAs(typeof(string), null));
                            }
                            //reader.Read();
                        }
                        if (reader.Name == "style")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _style.a = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _style[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _style.a = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _style[i] = Double.Parse(tokens[i]);
                            }
                            _style.a = _style.Reshape(_substyles.Count, _substyles.Count).T;
                        }
                        if (reader.Name == "styleu")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _styleu.a = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _styleu[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _styleu.a = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _styleu[i] = Double.Parse(tokens[i]);
                            }
                            _styleu.a = _styleu.Reshape(_substyles.Count, _substyles.Count).T;
                        }
                        if (reader.Name == "factorIdx")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _factorIdx.a = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _factorIdx[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _factorIdx.a = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _factorIdx[i] = Double.Parse(tokens[i]);
                            }
                        }
                        if (reader.Name == "factorIdxu")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _factorIdxu.a = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _factorIdxu[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _factorIdxu.a = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _factorIdxu[i] = Double.Parse(tokens[i]);
                            }
                        }
                    }
                    reader.Read();
                }
            }
            reader.Read();
        }

        public void Write(ref XmlWriter writer)
        {
            writer.WriteStartElement("Style");
                writer.WriteAttributeString("name", _name);

                writer.WriteStartElement("Data", null);
                    writer.WriteStartElement("Substyles");
                    int cnt = 0;
                    foreach (string s in _substyles)
                    {
                        writer.WriteAttributeString("styleName" + cnt.ToString(), s);
                        cnt++;
                    }
                    writer.WriteEndElement();

                    writer.WriteStartElement("style");
                        writer.WriteAttributeString("data", _style.ToString().Remove(0, _style.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                    writer.WriteEndElement();

                    if (!_styleu.IsEmpty)
                    {
                        writer.WriteStartElement("styleu");
                            writer.WriteAttributeString("data", _styleu.ToString().Remove(0, _style.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("factorIdx");
                        writer.WriteAttributeString("data", _factorIdx.ToString().Remove(0, _factorIdx.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                    writer.WriteEndElement();

                    if (!_factorIdxu.IsEmpty)
                    {
                        writer.WriteStartElement("factorIdxu");
                        writer.WriteAttributeString("data", _factorIdxu.ToString().Remove(0, _factorIdx.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
                        writer.WriteEndElement();
                    }

                writer.WriteEndElement();

            writer.WriteEndElement();
        }

        public bool ReadMat(string filename)
        {
            try
            {
                using (ILMatFile reader = new ILMatFile(filename))
                {
                    _style = reader.GetArray<double>("style");
                    _factorIdx = reader.GetArray<double>("FactorIdx");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                return false;
            }

            return true;
        }
    }
}
