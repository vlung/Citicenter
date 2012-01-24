using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TP
{
    /**
 * <p>
 * A generic ID that encapsulate a string.
 * You can compose two identifiers to create a hierarchy.
 * The internal representation is just a string. The hierarchy
 * is represented as a standard path (i.e., '/' delimited string).
 * </p>
 * 
 * <p>For example, you can use this class to represent a reservation record
 * by constructing a hierarchy of {@link Customer} and {@link RID}
 * as follow.</p>
 * 
 * <pre>
 * Customer cus	tomer;
 * RID resource;
 * LockableID id = new LockableID(customer,resource);
 * </pre>
 *
 * <p><b>Please feel free to modify this class.</b></p>
 */
public class LockableID :IComparable<LockableID>, Lockable {
    private String id;
    
    public LockableID(String id) {
        this.id = id;
    }
    
    public LockableID(Lockable item) {
        this.id = item.ToString();
    }

    public LockableID(String prefix,Lockable item) {
        this.id = prefix + "/" + item.ToString();
    }
    
    public LockableID(Lockable prefix,Lockable item) {
        this.id = prefix.ToString() + "/" + item.ToString();
    }
    
    public LockableID(Lockable prefix,String item) {
        this.id = prefix.ToString() + "/" + item;
    }
    
    /**
     * Construct a composite ID using this id as prefix.
     * @param other id to compose
     * @return a composite ID of this '/' other
     */
    public LockableID concat(Lockable other) {
        return new LockableID(this,other);
    }
    
    /**
     * check whether <code>this</code> object is prefix of the <code>other</code>.
     * @param other tested id
     * @return <code>true</code> if this object is prefix of the other.
     */
    public bool isPrefixOf(LockableID other) {
        if(id.Length <= other.id.Length)
            return false;
        return other.id[id.Length] == '/' && other.id.StartsWith(id);
    }

    /**
     * Get the prefix of this id.
     * @return the prefix of this id. <code>null</code> if this id is a root.
     */
    public LockableID getPrefix() {
        int pos = id.LastIndexOf('/');
        if ( pos < 0 ) return null;
        return new LockableID(id.Substring(0,pos));
    }

    /**
     * Get the last component of this id.
     * @return the last component of the path represented by this id.
     */
    public String getName() {
        int pos = id.LastIndexOf('/');
        return pos < 0 ? id : id.Substring(pos+1);
    }

    public override int GetHashCode() {
        return id.GetHashCode();
    }
    
    
    public override bool Equals(Object o) {
        if ( this == o ) return true;
        if ( o is LockableID ) {
            return id.Equals( ((LockableID)o).id );
        }
        return false;
    }
    
    
    public override string ToString() { return id; }


    public int CompareTo(LockableID o)
    {
        // we are very naive in comparison
        return id.CompareTo(o.id);
    }
}



}
