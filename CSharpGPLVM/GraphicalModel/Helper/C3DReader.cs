using ILNumerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GraphicalModel.Helper
{
    public class MarkerData
    {
        public double[] xyzData;
        public double cameraInfo;
        public double residualError;

        public MarkerData()
        {
            xyzData = new double[3];
        }
    }

    public class FrameData
    {
        public List<MarkerData> lMarkerData;
        private int frameNumber;


        public FrameData()
        {
            lMarkerData = new List<MarkerData>();
        }

        public int ID
        {
            get
            {
                return frameNumber;
            }
            set
            {
                frameNumber = value;
            }
        }
    }

   
    public class Parameter
    {
        private string desc;
        private string parameterName;
        public List<Object> parameterData;

        public Parameter()
        {
            parameterData = new List<Object>();
        }

        public string Description
        {
            get
            {
                return desc;

            }
            set
            {
                desc = value;
            }
        }

        public string Name
        {
            get
            {
                return parameterName;
            }
            set
            {
                parameterName = value;
            }
        }
    }

    public class Group
    {
        private int groupNumber;
        private string groupName;
        private string desc;
        public List<Parameter> lParameters;

        public Group()
        {
            lParameters = new List<Parameter>();
        }


        public int ID
        {
            get
            {
                return groupNumber;

            }
            set
            {
                groupNumber = value;
            }
        }

        public string Name
        {
            get
            {
                return groupName;
            }
            set
            {
                groupName = value;
            }
        }

        public string Description
        {
            get
            {
                return desc;
            }
            set
            {
                desc = value;
            }
        }

    }

    public class Event
    {
        private float eventTime;
        private string eventName;
        private bool eventFlag;

        public float Time
        {
            get
            {
                return eventTime;
            }
            set
            {
                eventTime = value;
            }
        }

        public string Name
        {
            get
            {
                return eventName;
            }
            set
            {
                eventName = value;
            }
        }

        public bool FlagValue
        {
            get
            {
                return eventFlag;
            }
            set
            {
                eventFlag = value;
            }
        }
    }

    public class C3DReader
    {
        private BinaryReader reader;
        private string fileName;
        private int position; //Stores the current position in terms of bytes
        private int length;
        private int parameterBlockIndex;
        private int numberOfMarkers;
        private int analogMeasurementsPer3DFrame;
        private int startFrameIndex;
        private int endFrameIndex;
        private int maxInterpolationGap;
        private float scaleFactor;
        private int dataBlockIndex;
        private int analogSamplesPer3DFrame;
        private int numberOfAnalogChannels;
        private float frameRate3D;
        private float analogFrameRate;
        private int numberOfEvents;
        List<Event> lEvents;
        List<Group> lGroups;
        List<FrameData> lFrameData;

        private const int CWORD_LENGTH = 2; //Length in Bytes;
        private const int CBLOCK_SIZE = 512; //Length in Bytes;

        public C3DReader()
        {
            lGroups = new List<Group>();
            lEvents = new List<Event>();
            lFrameData = new List<FrameData>();
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
        }

        public void OpenFileForReading(string fileName)
        {
            try
            {
                this.fileName = fileName;
                reader = new BinaryReader(File.Open(fileName,FileMode.Open));
                position = 0;
                length = (int)reader.BaseStream.Length;
                if(position >= length
                    || !fileName.EndsWith(".c3d"))
                    throw new Exception("Invalid File Type");

                // *****************  Header  Section  *********************
                // Read the header description section in C3D documentation
                // *****************  Header  Section  *********************
                
                parameterBlockIndex = ReadNextByte();
                if(ReadNextByte() != 80)
                    throw new Exception("Invalid File Type");

                numberOfMarkers = ReadNextInteger();
                analogMeasurementsPer3DFrame = ReadNextInteger();
                startFrameIndex = ReadNextInteger();
                endFrameIndex = ReadNextInteger();
                maxInterpolationGap = ReadNextInteger();
                scaleFactor = ReadNextFloat();
                dataBlockIndex = ReadNextInteger();
                analogSamplesPer3DFrame = ReadNextInteger();
                if (analogSamplesPer3DFrame > 0)
                    numberOfAnalogChannels = analogMeasurementsPer3DFrame / analogSamplesPer3DFrame;

                frameRate3D = ReadNextFloat();
                analogFrameRate = frameRate3D * analogSamplesPer3DFrame;

                //reader.BaseStream.Seek(148 * CWORD_LENGTH, System.IO.SeekOrigin.Begin);
                SeekWord(false, 150);

                //********************* Header Events **********************
                //Read Header Events Section of C3D Documentation
                //********************* header Events **********************

                if (ReadNextInteger() == 12345)
                {
                    numberOfEvents = ReadNextInteger();
                    if (numberOfEvents > 0)
                    {
                        
                        //Initialize List
                        for(int i=0; i<numberOfEvents; i++)
                            lEvents.Add(new Event());

                        SeekWord(false, 153);
                        for(int i=0; i<numberOfEvents; i++)
                            lEvents[i].Time = ReadNextFloat();

                        SeekWord(false, 189);
                        for (int i = 0; i < numberOfEvents; i++)
                            lEvents[i].FlagValue = Convert.ToBoolean(ReadNextInteger());

                        SeekWord(false, 199);
                        for (int i = 0; i < numberOfEvents; i++)
                            lEvents[i].Name = ReadNextString(4);



                    }
                }

                //*************************END******************************

                //*************************END******************************

                
                // ***************** Parameter Section *********************
                //Read Parameter Header Section of C3D format documentation for more details
                // ***************** Parameter Section *********************

                //reader.BaseStream.Seek(CBLOCK_SIZE * (parameterBlockIndex - 1) + CWORD_LENGTH, System.IO.SeekOrigin.Begin);
                SeekByte(false, 3, parameterBlockIndex);
                int numberOfParamBlocks = ReadNextByte();
                int processorType = ReadNextByte() - 83;

                if (processorType == 2)
                {
                    throw new Exception("Doesn't support Vax Ordering");
                }

                while (true)
                {
                    int numberOfCharactersInName = ReadNextByte();
                    if (numberOfCharactersInName == 0)
                        break;
                    int recordNumber = ReadNextByte();
                    if (recordNumber > 0)
                    {
                        //Parameter record
                        Parameter prm = new Parameter();
                        Group grp;
                        grp = lGroups.Find(
                                        delegate(Group gp)
                                        {
                                            return gp.ID == recordNumber;
                                        }
                               );
                        if (grp == null)
                        {
                            grp = new Group();
                            grp.ID = recordNumber;
                            lGroups.Add(grp);
                        }
                        grp.lParameters.Add(prm);
                        prm.Name = ReadNextString(numberOfCharactersInName);
                        int offset = ReadNextInteger();
                        int lengthOfDataElement = ReadNextByte();
                        int dimensions = ReadNextByte();
                        int numberOfElements = 1;
                        int[] lengthOfDimension = new int[dimensions];
                        for (int i = 0; i < dimensions; i++)
                        {
                            lengthOfDimension[i] = ReadNextByte();
                            numberOfElements *= lengthOfDimension[i];
                        }
                        if (grp.ID == 7 && String.Equals(prm.Name,"LABELS"))
                            grp.ID = 7;

                        ILArray<double> data = new double[numberOfElements];
                        switch (lengthOfDataElement)
                        {
                            case -1:
                                if (lengthOfDimension.Length > 2)
                                    throw new Exception("Weird String Format");
                                else if (lengthOfDimension.Length < 2)
                                {
                                    prm.parameterData.Add(ReadNextString(numberOfElements));
                                }
                                else
                                {
                                    for (int i = 0; i < lengthOfDimension[1]; i++)
                                    {
                                        prm.parameterData.Add(ReadNextString(lengthOfDimension[0]));
                                    }
                                }
                                break;
                            case 1:
                                for (int i = 0; i < numberOfElements; i++)
                                {
                                    prm.parameterData.Add(ReadNextByte());
                                }
                                break;
                            case 2:
                                for (int i = 0; i < numberOfElements; i++)
                                {
                                     prm.parameterData.Add(ReadNextInteger());
                                }
                                break;
                            case 4:
                                for (int i = 0; i < numberOfElements; i++)
                                {
                                     prm.parameterData.Add(ReadNextFloat());
                                }
                                break;
                        }
                        int numberOfCharsInDescription = ReadNextByte();
                        prm.Description = ReadNextString(numberOfCharsInDescription);
                        //SeekByte(true, offset);

                    }
                    else
                    {
                        //Group record
                        Group grp;
                        grp = lGroups.Find(
                                        delegate(Group gp)
                                        {
                                            return gp.ID == Math.Abs(recordNumber);
                                        }
                                    );
                        if (grp == null)
                            grp = new Group();
                        grp.ID = Math.Abs(recordNumber);
                        grp.Name = ReadNextString(numberOfCharactersInName);
                        int offset = ReadNextInteger();
                        int numberOfCharsInDescription = ReadNextByte();
                        grp.Description = ReadNextString(numberOfCharsInDescription);
                        lGroups.Add(grp);
                        //Because it's offset from the start of the block
                        SeekByte(true, offset-3-numberOfCharsInDescription);

                    }

                }

                //*************************END******************************
   
                //**********************DATA SECTION************************
                //Read Data Format in C3D documentation for more details
                //**********************DATA SECTION************************

                SeekByte(false, 1, dataBlockIndex);
                int numberOfFrames = endFrameIndex - startFrameIndex +1;
                
                if (scaleFactor < 0)
                {
                    for (int i = startFrameIndex; i < endFrameIndex; i++)
                    {
                        FrameData frmDta = new FrameData();
                        frmDta.ID = i;
                        for (int j = 0; j < numberOfMarkers; j++)
                        {
                            MarkerData mkrData = new MarkerData();
                            mkrData.xyzData[0] = ReadNextFloat();
                            mkrData.xyzData[1] = ReadNextFloat();
                            mkrData.xyzData[2] = ReadNextFloat();
                            frmDta.lMarkerData.Add(mkrData);
                            double temp = ReadNextFloat();
                            temp = temp < 0 ? Math.Ceiling(temp) : Math.Floor(temp);  
                        }
                        lFrameData.Add(frmDta);
                    }

                }
            }
            catch (Exception e)
            {
                reader.Close();
                throw new Exception(fileName + ": " + e.Message);
            }
        }

        public List<FrameData> ReadFrameData()
        {
            return lFrameData;
        }

        public List<Event> ReadEventData()
        {
            return lEvents;
        }

        public List<Group> ReadGroupData()
        {
            return lGroups;
        }

        public sbyte ReadNextByte()
        {
            try
            {
                position += sizeof(sbyte);
                return reader.ReadSByte();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to read the next byte:" + e.Message );
            }
        }

        public Int16 ReadNextInteger()
        {
            try
            {
                position += sizeof(Int16);
                return reader.ReadInt16();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to read next integer word:" + e.Message);
            }
        }

        public float ReadNextFloat()
        {
            try
            {
                position += sizeof(Single);
                return reader.ReadSingle();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to read next floating point data:" + e.Message);
            }
        }

        public string ReadNextString(int numberOfChars)
        {
            try
            {
                if (numberOfChars == 0)
                    return "";

                position += numberOfChars;
                return new String(reader.ReadChars(numberOfChars));
            }
            catch (Exception e)
            {
                throw new Exception("Unable to read next integer word:" + e.Message);
            }
        }


        public void SeekWord(bool fromCurrent, int offset, int blockNumber = 1)
        {
            try
            {
                if (!fromCurrent)
                {
                    position = CBLOCK_SIZE * (blockNumber - 1) + (offset - 1) * CWORD_LENGTH;
                    reader.BaseStream.Seek(position, System.IO.SeekOrigin.Begin);
                }
                else
                {
                    position += (offset * CWORD_LENGTH);
                    reader.BaseStream.Seek((offset * CWORD_LENGTH), System.IO.SeekOrigin.Current);
                }

            }
            catch (Exception e)
            {
                throw new Exception("Unable to seek the desired Word. " + e.Message);
            }
        }

        public void SeekByte(bool fromCurrent, int offset, int blockNumber = 1)
        {
            try
            {
                if (!fromCurrent)
                {
                    position = CBLOCK_SIZE * (blockNumber - 1) + (offset - 1);
                    reader.BaseStream.Seek(position, System.IO.SeekOrigin.Begin);
                }
                else
                {
                    position += (offset) ;
                    reader.BaseStream.Seek(offset, System.IO.SeekOrigin.Current);
                }

            }
            catch (Exception e)
            {
                throw new Exception("Unable to seek the desired Word. " + e.Message);
            }
        }

    }
}
