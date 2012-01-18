﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using ExtensionMethods;

namespace TP
{
    /** 
     * The class represents a resource identifier.
     * 
     * There are three types of resource.
     * <ul>
     * <li>Flight</li>
     * <li>Room</li>
     * <li>Car</li>
     * </ul>
     * 
     * The external string representation ({@link #toString()}) of this identifier is following.
     * 
     * <p>
     * <i>prefix ':' identifier</i>
     * </p>
     * 
     * Valid prefix is 'F', 'R', 'C', which correspond to Flight, Room, and Car.
     * {@link #getName()} returns the identifier part.
     */
    [System.Serializable()]
    public class RID : IComparable<RID>
    {
        #region Constants

        private const long serialVersionUID = 7717150775758337149L;
        private const int MAX_NAME_LENGTH = 24;

        public const String _prefixes = "!FRC";

        #endregion

        #region Member Variables

        private Type type;
        private String name;

        #endregion

        /**
         * resource type
         */
        public enum Type
        {

            INVALID,
            FLIGHT,
            ROOM,
            CAR,

        }

        #region Public Methods

        public RID()
        {
            type = Type.INVALID;
            name = "NA";
        }

        public RID(Type type, String name)
        {
            if (type == Type.INVALID || string.IsNullOrEmpty(name) || name.Length > MAX_NAME_LENGTH)
            {
                throw new ArgumentException();
            }
            this.type = type;
            this.name = name;
        }

        /**
         * create identifier with flight number
         * @param flight the flight number
         */
        public RID(int flight)
        {
            this.type = Type.FLIGHT;
            this.name = flight.ToString();
        }

        /**
         * get type of resource id
         * @return type value of current identifier
         */
        public Type getType() { return type; }

        /**
         * @return name of resource
         */
        public String getName() { return name; }


        public override int GetHashCode()
        {
            return name.GetHashCode() + type.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o is RID)
            {
                RID other = (RID)o;
                return type == other.type && name.Equals(other.name);
            }
            return false;
        }


        public int CompareTo(RID o)
        {
            int cmp = type.CompareTo(o.type);
            return cmp == 0 ? name.CompareTo(o.name) : cmp;
        }


        public override String ToString()
        {
            return type.prefix() + ":" + name;
        }

        /**
         * convenient method to create flight resource identifier
         * @param flight flight number
         * @return flight resource identifier
         */
        public static RID forFlight(int flight)
        {
            return new RID(flight);
        }

        /**
         * convenient method to create flight resource identifier
         * @param flight flight number in string
         * @return flight resource identifier
         */
        public static RID forFlight(String flight)
        {
            return new RID(Type.FLIGHT, flight);
        }

        /**
         * convenient method to create car resource identifier
         * @param loc location
         * @return car resource identifier
         */
        public static RID forCar(String loc)
        {
            return new RID(Type.CAR, loc);
        }

        /**
         * convenient method to create room resource identifier
         * @param loc location
         * @return room resource identifier
         */
        public static RID forRoom(String loc)
        {
            return new RID(Type.ROOM, loc);
        }

        /**
         * parse external string representation
         * @param s result of {@link #toString()}
         * @return newly created resource identifier
         */
        public static RID parse(String s)
        {

            Type t = getInstance(s);
            String id = s.Substring(2);
            return new RID(t, id);
        }

        #endregion

        #region Private Methods

        private static Type getInstance(String s)
        {
            char ch = s[0];
            int ord = _prefixes.IndexOf(ch);
            return (TP.RID.Type)Enum.GetValues(typeof(TP.RID.Type)).GetValue(ord);
        }

        /*private void writeObject(java.io.ObjectOutputStream out) throws IOException {
            out.writeUTF(this.toString());
        }
    
        private void readObject(Stream _in)
        {
            String s = _in.rereadUTF();
            type = Type.getInstance(s);
            name = s.substring(2);
        }*/

        #endregion
    }
}

namespace ExtensionMethods
{

    public static class TypeExtensions
    {
        public static char prefix(this TP.RID.Type _type)
        {
            return TP.RID._prefixes[(int)_type];
        }
    }
}
