using System;
using System.Collections.Generic;
using System.Text;
using ILNumerics;
using GPLVM.Dynamics.Topology;

namespace MotionPrimitives
{
    public class TupleList<T1, T2> : List<Tuple<T1, T2>>
    {
        public void Add(T1 item, T2 item2)
        {
            Add(new Tuple<T1, T2>(item, item2));
        }
    }

    public class NamedEntity
    {
        private string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
        }
        public NamedEntity(string name)
        {
            this._Name = name;
        }
    }

    public class EventType : NamedEntity
    {
        public EventType(string name)
            : base(name)
        {
        }
    }

    public class EventTypes
    {
        private Dictionary<int, EventType> map = new Dictionary<int, EventType>();
        private int _n = 0;

        public EventType FindEventTypeByID(int id)
        {
            return map[id];
        }
        public void RegisterNewEventType(string name)
        {
            map.Add(_n, new EventType(name));
            _n++;
        }
    }

    public class EventMarker
    {
        public EventType Event;
        public int Frame;
    }

    public class MotionPrimitiveType : NamedEntity
    {
        public MotionPrimitiveType(string name)
            : base(name)
        {
        }
    }

    public class MotionPrimitiveTypes
    {
        private Dictionary<int, MotionPrimitiveType> map = new Dictionary<int, MotionPrimitiveType>();
        private int _n = 0;

        public MotionPrimitiveType FindEventTypeByID(int id)
        {
            return map[id];
        }
        public MotionPrimitiveType RegisterNewMotionPrimitiveType(string name)
        {
            MotionPrimitiveType newType = new MotionPrimitiveType(name);
            map.Add(_n, newType);
            _n++;
            return newType;
        }
    }

    public class MotionPrimitiveMarker
    {
        public int FrameStart;
        public MotionPrimitiveType Type;
        public MotionPrimitiveMarker(int frameStart, MotionPrimitiveType primitiveType)
        {
            Type = primitiveType;
            FrameStart = frameStart;
        }
    }

    public class BodySegment
    {
        public string Name;
        public ILArray<int> Channels = ILMath.empty<int>();
    }

    public class BodySegmentsMap : List<BodySegment>
    {
        public ILRetCell FramesToSegments(ILInArray<double> inFullFrames)
        {
            using (ILScope.Enter(inFullFrames))
            {
                ILArray<double> frames = ILMath.check(inFullFrames);
                ILCell segments = ILMath.cell(this.Count);
                for (int k = 0; k < this.Count; k++)
                {
                    segments[k] = frames[ILMath.full, this[k].Channels];
                }
                return segments;
            }
        }

        public ILRetArray<double> SegmentsToFrames(ILInCell inSegments)
        {
            using (ILScope.Enter(inSegments))
            {
                ILCell segments = ILMath.check(inSegments);
                ILArray<double> fullFrames = ILMath.zeros<double>(segments.GetArray<double>(0).S);
                for (int k = 0; k < this.Count; k++)
                {
                    fullFrames[ILMath.full, this[k].Channels] = segments.GetArray<double>(k);
                }
                return fullFrames;
            }
        }
    }

    // Motion primitives marker list that describes single body part
    public class MotionPrimitiveMarkerList : List<MotionPrimitiveMarker>
    {
        public BodySegment BodySegmentLink;
    }

    public class CategoryType : NamedEntity
    {
        private CategoryValues _CategoryValues = new CategoryValues();

        public CategoryType(string name)
            : base(name)
        {
        }

        public CategoryValues Values
        {
            get { return _CategoryValues; }
        }
    }

    public class CategoryTypes : List<CategoryType>
    {
    }

    public class CategoryValue : NamedEntity
    {
        private CategoryType pCategoryType;

        public CategoryValue(CategoryType categoryType, string name)
            : base(name)
        {
            pCategoryType = categoryType;
            pCategoryType.Values.Add(this);
        }
    }

    public class CategoryValues : List<CategoryValue>
    {
    }

    public class Trial
    {
        public CategoryValues Tags = new CategoryValues();
        public List<EventMarker> Events = new List<EventMarker>();
        public List<MotionPrimitiveMarkerList> PrimitiveMarkers = new List<MotionPrimitiveMarkerList>(); // for every body segment
        public ILArray<double> Frames = ILMath.empty<double>();
    }

    public class GPDMInput
    {
        public ILArray<double> Data = ILMath.empty<double>();
        public GPTopology Topology = new GPTopology();
    }
    
    public class MotionDataSet
    {
        //public Skeleton
        public EventTypes lEventTypes = new EventTypes();
        public MotionPrimitiveTypes lPrimitiveTypes = new MotionPrimitiveTypes();
        public BodySegmentsMap lBodySegmentsMap = new BodySegmentsMap();

        public List<Trial> Trials = new List<Trial>();

        public MotionDataSet()
        {
        }

        private ILRetArray<double> ChunkByBodySegment(ILInArray<double> inFullFrames, BodySegment bodySegment)
        {
            using (ILScope.Enter(inFullFrames))
            {
                ILArray<double> frames = ILMath.check(inFullFrames);
                ILArray<double> dataChunk = frames[ILMath.full, bodySegment.Channels];
                return dataChunk;
            }
        }

        public List<GPDMInput> CreateGPDMInput()
        {
            List<GPDMInput> lGPDMInput = new List<GPDMInput>();

            int k = 0;
            foreach (var bodySegment in lBodySegmentsMap)
            {
                GPDMInput gpdmInput = new GPDMInput();
                gpdmInput.Data.a = ILMath.zeros(0, bodySegment.Channels.Length);
                foreach (var tr in Trials)
                {
                    ILArray<double> dataChunk = ChunkByBodySegment(tr.Frames, bodySegment);
                    gpdmInput.Data = gpdmInput.Data.Concat(dataChunk, 0);

                    ILArray<int> segmentSize = ILMath.zeros<int>(tr.PrimitiveMarkers[k].Count, 1);
                    for (int k1 = 0; k1 < tr.PrimitiveMarkers[k].Count - 1; k1++)
                    {
                        segmentSize[k1] = tr.PrimitiveMarkers[k][k1 + 1].FrameStart - tr.PrimitiveMarkers[k][k1].FrameStart;
                    }
                    segmentSize[ILMath.end] = gpdmInput.Data.S[0] - tr.PrimitiveMarkers[k][tr.PrimitiveMarkers[k].Count - 1].FrameStart;
                    foreach (var s in segmentSize)
                    {
                        gpdmInput.Topology.AddSegment(s);
                    }

                }
            }
            return lGPDMInput;
        }

    }
}
