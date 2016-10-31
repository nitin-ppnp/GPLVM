using System;
using System.Collections.Generic;
using ILNumerics;
using System.Xml;

namespace GPLVM.Styles
{
    public class TopologicalPosition
    {
        private List<string> _nameList;
        private ILArray<double> _coordinates;
        private ILArray<double> _coordinatesU;
        private ILArray<double> _factorIdx;
        private ILArray<double> _factorIdxu;

        private int _q;

        public TopologicalPosition()
        {
            _coordinates = ILMath.empty();
            _coordinatesU = ILMath.empty();
            _nameList = new List<string>();

            _factorIdx = ILMath.empty();
            _factorIdxu = ILMath.empty();
        }

        public int NumParameter
        {
            get
            {
                int numParam = _coordinates.S[0] * _coordinates.S[1];
                if (!_coordinatesU.IsEmpty)
                    numParam += _coordinatesU.Size[0] * _coordinatesU.Size[1];
                return numParam;
            }
        }

        public ILArray<double> LogParameter
        {
            get
            {
                ILArray<double> tmp = ILMath.empty();
                tmp = _coordinates[ILMath.full].T.C;
                if (!_coordinatesU.IsEmpty)
                    tmp[ILMath.r(ILMath.end + 1, ILMath.end + _coordinatesU[ILMath.full].T.Length)] = _coordinatesU[ILMath.full].T.C;
                return tmp.C;
            }
            set
            {
                ILArray<double> param = ILMath.empty();
                param.a = value;

                int startVal = 0;
                int endVal = _coordinates.S[0] * _coordinates.S[1] - 1;

                _coordinates = ILMath.reshape(param[ILMath.r(startVal, endVal)], _coordinates.S[0], _coordinates.S[1]);

                if (!_coordinatesU.IsEmpty)
                {
                    startVal = endVal + 1;
                    endVal += _coordinatesU.S[0] * _coordinatesU.S[1];

                    _coordinatesU = ILMath.reshape(param[ILMath.r(startVal, endVal)], _coordinatesU.S[0], _coordinatesU.S[1]);
                }
            }
        }

        public ILArray<double> FactorIndex
        {
            get
            {
                return _factorIdx;
            }
        }

        public int Dimension
        {
            get
            {
                return _q;
            }
        }

        public ILArray<double> FactorIndexInducing
        {
            get
            {
                return _factorIdxu;
            }
            set
            {
                _factorIdxu = value.C;
                _coordinatesU = _coordinates.C;
            }
        }

        public ILArray<double> Coordinates
        {
            get
            {
                return _coordinates;
            }
        }

        public void AddPosition(ILArray<double> pos, string name, int seqenceLength)
        {
            if (pos.S[0] > pos.S[1])
                pos = pos.T;

            if (!_nameList.Contains(name))
            {
                _nameList.Add(name);
                if (_factorIdx.IsEmpty)
                {
                    _factorIdx = ILMath.zeros(1, seqenceLength);
                    _coordinates = pos.C;
                    _q = _coordinates.S[1];
                }
                else
                {
                    _factorIdx[ILMath.r(ILMath.end + 1, ILMath.end + seqenceLength)] = _nameList.IndexOf(name);
                    _coordinates[ILMath.end + 1, ILMath.full] = pos.C;
                }
            }
            else
            {
                int index = _nameList.IndexOf(name);
                _factorIdx[ILMath.r(ILMath.end + 1, ILMath.end + seqenceLength)] = index;
            }
        }

        public ILRetArray<double> PosGradient(ILInArray<double> gPosSequence)
        {
            using (ILScope.Enter(gPosSequence))
            {
                ILArray<double> gPS = ILMath.check(gPosSequence);
                ILArray<double> gS = ILMath.zeros(_coordinates.Size);
                for (int i = 0; i < _nameList.Count; i++)
                    gS[i, ILMath.full] = ILMath.sum(gPS[ILMath.find(_factorIdx == i), ILMath.full], 0);

                ILRetArray<double> res = gS;

                return res[ILMath.full].T;
            }
        }

        public ILRetArray<double> PosInducingGradient(ILInArray<double> gPosSequence)
        {
            using (ILScope.Enter(gPosSequence))
            {
                ILArray<double> gPS = ILMath.check(gPosSequence);
                ILArray<double> gS = ILMath.zeros(_coordinates.Size);
                for (int i = 0; i < _nameList.Count; i++)
                    gS[i, ILMath.full] = ILMath.sum(gPS[ILMath.find(_factorIdxu == i), ILMath.full], 0);

                ILRetArray<double> res = gS;

                return res[ILMath.full].T;
            }
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
                        if (reader.Name == "q")
                        {
                            while (reader.NodeType != XmlNodeType.EndElement)
                            {
                                reader.Read();
                                if (reader.NodeType == XmlNodeType.Text)
                                    _q = (int)Double.Parse(reader.Value);

                            }
                            reader.Read();
                        }
                        if (reader.Name == "PositionNames")
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                //reader.Read();
                                _nameList.Add((string)reader.ReadContentAs(typeof(string), null));
                            }
                            //reader.Read();
                        }
                        if (reader.Name == "coordinates")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _coordinates = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _coordinates[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _coordinates = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _coordinates[i] = Double.Parse(tokens[i]);
                            }
                            _coordinates = _coordinates.Reshape(_q, _nameList.Count).T;
                        }
                        if (reader.Name == "coordinatesU")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _coordinatesU = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _coordinatesU[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _coordinatesU = ILMath.zeros(1, tokens.Length);
                                for (int i = 0; i < tokens.Length; i++)
                                    _coordinatesU[i] = Double.Parse(tokens[i]);
                            }
                            _coordinatesU = _coordinatesU.Reshape(_q, _nameList.Count).T;
                        }
                        if (reader.Name == "factorIdx")
                        {
                            reader.MoveToAttribute("data");
                            tokens = (string[])reader.ReadContentAs(typeof(string[]), null);
                            if (tokens[2] == "*")
                            {
                                _factorIdx = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _factorIdx[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _factorIdx = ILMath.zeros(1, tokens.Length);
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
                                _factorIdxu = ILMath.zeros(1, tokens.Length - 3);
                                for (int i = 0; i < tokens.Length - 3; i++)
                                    _factorIdxu[i] = Double.Parse(tokens[i + 3]) * Double.Parse(tokens[1]);
                            }
                            else
                            {
                                _factorIdxu = ILMath.zeros(1, tokens.Length);
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
            writer.WriteStartElement("TopologicalPosition");

            writer.WriteStartElement("Data", null);
            writer.WriteElementString("q", _q.ToString());
            writer.WriteStartElement("PositionNames");
            int cnt = 0;
            foreach (string s in _nameList)
            {
                writer.WriteAttributeString("PosName" + cnt.ToString(), s);
                cnt++;
            }
            writer.WriteEndElement();

            writer.WriteStartElement("coordinates");
            writer.WriteAttributeString("data", _coordinates.ToString().Remove(0, _coordinates.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
            writer.WriteEndElement();

            if (!_coordinatesU.IsEmpty)
            {
                writer.WriteStartElement("coordinatesU");
                writer.WriteAttributeString("data", _coordinatesU.ToString().Remove(0, _coordinatesU.ToString().IndexOf("]") + 1).Replace("\n", "").Replace("\r", ""));
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
    }
}
