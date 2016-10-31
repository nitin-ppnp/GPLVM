using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ILNumerics;

namespace FactorGraph.Utils
{
    public class Serializer
    {
        public static Object Clone(Object source, Type type)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Serialize(source, stream);
                stream.Position = 0;
                return Deserialize(type, stream);
            }
        }

        public static void Serialize(Object obj, string filename)
        {                        
            FileStream fs = new FileStream(filename, FileMode.Create);
            try
            {
                Serialize(obj, fs);
            }
            catch (SerializationException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        public static void Serialize(Object obj, Stream output)
        {
            DataContractSerializer surrogateSerializer = CreateSurrogateSerializer(obj.GetType());
            //var settings = new XmlWriterSettings { Indent = false };
            var settings = new XmlWriterSettings { Indent = true };
            var writer = XmlWriter.Create(output, settings);
            try
            {
                surrogateSerializer.WriteObject(writer, obj);
            }
            catch (SerializationException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            finally
            {
                writer.Close();
            }
        }

        public static Object Deserialize(Type type, string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open);
            try
            {
                Object obj = Deserialize(type, fs);              
                return obj;
            }
            catch (SerializationException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        public static Object Deserialize(Type type, Stream output)
        {
            var quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxStringContentLength = 10 * 1024 * 1024;
            quotas.MaxDepth = 1000000;
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(output, quotas);

            try
            {
                DataContractSerializer surrogateSerializer = CreateSurrogateSerializer(type);
                Object obj = surrogateSerializer.ReadObject(reader, false);
                return obj;
            }
            catch (SerializationException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            finally
            {
                reader.Close();                
            }
        }

        static DataContractSerializer CreateSurrogateSerializer(Type type)
        {
            // Create a Generic List for the knownTypes. 
            List<Type> knownTypes = new List<Type>();
            knownTypes.Add(typeof(ILArray<int>));
            knownTypes.Add(typeof(ILArray<double>));
            knownTypes.Add(typeof(int[]));
            knownTypes.Add(typeof(double[]));

            ArrayTypesSurrogate surrogate = new ArrayTypesSurrogate();
            DataContractSerializer surrogateSerializer =
                new DataContractSerializer(type, knownTypes, Int16.MaxValue, false, true, surrogate);
            return surrogateSerializer;
        }
    }

    //========================================================================================
    // Surrogate types
    [DataContract(IsReference = true)]
    public class ILNumericsArraySurrogate<T>
    {
        [DataMember()]
        public int[] aSize;
        [DataMember()]
        public T[] aData;
    }

    [DataContract()]
    public class DoubleArraySurrogate
    {
        [DataMember()]
        public string Encoding = "G17"; //"Int64"; // Use "Int64" for bitwise precision or "G17" for human-readable format
        [DataMember()]
        public string SerializedDoubleArray;

        public double[] DoubleArray
        {
            get 
            {
                if (SerializedDoubleArray.Length == 0)
                    return new double[0];
                else if (Encoding == "Int64")
                    return SerializedDoubleArray.Split(' ').Select(x => BitConverter.Int64BitsToDouble(Int64.Parse(x))).ToArray(); 
                else
                    return SerializedDoubleArray.Split(' ').Select(x => double.Parse(x)).ToArray();
            }
            set 
            { 
                if (value.Length == 0) 
                    SerializedDoubleArray = "";
                else if (Encoding == "Int64")
                    SerializedDoubleArray = value.Select(x => BitConverter.DoubleToInt64Bits(x).ToString()).Aggregate((x, y) => x + " " + y); 
                else
                    SerializedDoubleArray = value.Select(x => x.ToString(Encoding)).Aggregate((x, y) => x + " " + y);
            }
        }
    }

    [DataContract()]
    public class IntArraySurrogate
    {
        [DataMember()]
        public string SerializedIntArray;

        public int[] IntArray
        {
            get { return SerializedIntArray.Length == 0 ? new int[0] : SerializedIntArray.Split(' ').Select(x => int.Parse(x)).ToArray(); }
            set { SerializedIntArray = value.Length == 0 ? "" : value.Select(x => x.ToString()).Aggregate((x, y) => x + " " + y); }
        }
    }

    //========================================================================================

    // This is the surrogate that substitutes some types
    public class ArrayTypesSurrogate : IDataContractSurrogate
    {
        public Type GetDataContractType(Type type)
        {
            if (type.Equals(typeof(ILArray<double>)))
                return typeof(ILNumericsArraySurrogate<double>);
            if (type.Equals(typeof(ILArray<int>)))
                return typeof(ILNumericsArraySurrogate<int>);
            if (type.Equals(typeof(int[])))
                return typeof(IntArraySurrogate);
            if (type.Equals(typeof(double[])))
                return typeof(DoubleArraySurrogate);
            return type;
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            if (obj is ILArray<int>)
            {
                ILArray<int> original = (ILArray<int>)obj;
                var surrogate = new ILNumericsArraySurrogate<int>();
                surrogate.aSize = original.S.ToIntArray();
                surrogate.aData = new int[original.S.NumberOfElements];
                original.ExportValues(ref surrogate.aData);
                return surrogate;
            }
            else if (obj is ILArray<double>)
            {
                ILArray<double> original = (ILArray<double>)obj;
                var surrogate = new ILNumericsArraySurrogate<double>();
                surrogate.aSize = original.S.ToIntArray();
                surrogate.aData = new double[original.S.NumberOfElements];
                original.ExportValues(ref surrogate.aData);
                return surrogate;
            }
            else if (obj is int[])
            {
                var original = (int[])obj;
                var surrogate = new IntArraySurrogate();
                surrogate.IntArray = original;
                return surrogate;
            }
            else if (obj is double[])
            {
                var original = (double[])obj;
                var surrogate = new DoubleArraySurrogate();
                surrogate.DoubleArray = original;
                return surrogate;
            }
            return obj;
        }

        public object GetDeserializedObject(Object obj, Type targetType)
        {            
            if (obj is ILNumericsArraySurrogate<int>)
            {
                var surrogate = (ILNumericsArraySurrogate<int>)obj;
                ILArray<int> original = ILMath.localMember<int>();
                original.a = surrogate.aData;
                original.a = original.Reshape(surrogate.aSize);
                return original;
            }
            else if (obj is ILNumericsArraySurrogate<double>)
            {
                var surrogate = (ILNumericsArraySurrogate<double>)obj;
                ILArray<double> original = ILMath.localMember<double>();
                original.a = surrogate.aData;
                original.a = original.Reshape(surrogate.aSize);
                return original;
            }
            else if (obj is IntArraySurrogate)
            {
                var surrogate = (IntArraySurrogate)obj;
                int[] original = surrogate.IntArray;
                return original;
            }
            else if (obj is DoubleArraySurrogate)
            {
                var surrogate = (DoubleArraySurrogate)obj;
                double[] original = surrogate.DoubleArray;
                return original;
            }
            return obj;
        }

        public Type GetReferencedTypeOnImport(string typeName,
            string typeNamespace, object customData)
        {
            // This method is called on schema import.
            // If a PersonSurrogated data contract is 
            // in the specified namespace, do not create a new type for it 
            // because there is already an existing type, "Person".
            throw new NotImplementedException();
            //return null;
        }

        public System.CodeDom.CodeTypeDeclaration ProcessImportedType(
            System.CodeDom.CodeTypeDeclaration typeDeclaration,
            System.CodeDom.CodeCompileUnit compileUnit)
        {
            // Console.WriteLine("ProcessImportedType invoked")
            // Not used in this sample.
            // You could use this method to construct an entirely new CLR 
            // type when a certain type is imported, or modify a 
            // generated type in some way.
            throw new NotImplementedException();
            //return typeDeclaration;
        }

        public object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            throw new NotImplementedException();
            //return null;
        }

        public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType)
        {
            throw new NotImplementedException();
            //return null;
        }

        public void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
        {
            throw new NotImplementedException();
        }
    }
}
