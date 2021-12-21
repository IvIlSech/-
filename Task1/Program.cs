using System;
using System.Numerics;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;

public delegate Vector2 FdblVector2( double x, double y );

struct DataItem 
{
	public double X { get; set; }
	public double Y { get; set; }
	public System.Numerics.Vector2 E { get; set; }
	public DataItem( double a, double b, System.Numerics.Vector2 c)
	{
		this.X = a;
		this.Y = b;
		this.E = c;
	}   
	public override string ToString()
	{
		return string.Format("{0},{1},{2},{3},{4}"
				, X, Y, E.X, E.Y, E.Length());
	} 
	public string ToLongString( string format )
	{
		string str_out = "";
		str_out += " x=" + String.Format( format, X);
		str_out += " y=" + String.Format( format, Y);
		str_out += " Ex=" + String.Format( format, E.X);
		str_out += " Ey=" + String.Format( format, E.Y);
		str_out += " |E|=" +String.Format( format, E.Length()) + "\n";
		return str_out;
	}
}

abstract class V3Data: IEnumerable<DataItem>
{
	public string name { get; protected set; }
	public DateTime dttm { get; protected set; }
	public V3Data( string str, DateTime date )
	{
		this.name = str;
		this.dttm = date;
	}
	public abstract int Count { get; }
	public abstract double MaxDistance { get; }
	public abstract string ToLongString( string format );
	public override string ToString()
	{
		return( String.Format("{0],{1}", name, dttm ));
	}
	public abstract IEnumerator<DataItem> GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator()
	{ return this.GetEnumerator(); }
}

class V3DataList : V3Data
{
	public List<DataItem> lst { get; private set;}
	public override double MaxDistance 
	{ 
		get
		{
			double maxd = 0;
			foreach ( DataItem i in lst )
			{
				foreach ( DataItem p in lst )
				{
					double x = i.X - p.X;
					double y = i.Y - p.Y;
					float a = Convert.ToSingle(x);
					float b = Convert.ToSingle(y);
					Vector2 vec = new Vector2(a,b);
					if( vec.Length() > maxd)
					{
						maxd = vec.Length();
					}
				}
			}
			return maxd;
		}
	}
	public override int Count { get { return lst.Count; } }
	public V3DataList( string str, DateTime date ):base(str,date)
	{
		this.lst = new List<DataItem>();
	}
	public bool Add( DataItem newItem )
	{
		foreach ( DataItem i in lst )
		{
			float a = Convert.ToSingle(i.X - newItem.X);
			float b = Convert.ToSingle(i.Y - newItem.Y);
			Vector2 vec = new Vector2( a, b );
			if( vec.Length() == 0)
			{
				return false;
			}
		}
		this.lst.Add( newItem );
		return true;
	}
	public int AddDefaults( int nItems, FdblVector2 F)
	{
		double x = 0, y = 0, dx = 0.1;
		int i, counter = 0;
		x = this.Count*dx;
		for( i = 1; i <= nItems; i++ )
		{
			x = x + i*dx;
			y = x*x/3;
			DataItem nItem = new DataItem( x, y, F(x,y));
			if( Add( nItem ) )
			{
				counter++;
			}
		}
		return counter;
	}
	public override string ToString()
	{
		return String.Format("V3DataLIst,{0},{1},{2},{3}\n",
			base.name, base.dttm, this.Count, this.MaxDistance);
	}
	public override string ToLongString( string format )
	{
		string out_str = this.ToString();
		foreach ( DataItem p in this.lst )
		{
			out_str = out_str + p.ToLongString( format ); 
		}
		return out_str;
	}
	public override IEnumerator<DataItem> GetEnumerator()
	{
		return lst.GetEnumerator();
	}
	public static bool SaveAsBinary( string filename, V3DataList v3 )
	{
		FileStream fs = null;
		try
		{
			fs = new FileStream( filename, FileMode.Open);
			BinaryWriter sv = new BinaryWriter(fs);
			sv.Write(v3.name);
			sv.Write(v3.dttm.ToBinary());
			sv.Write(v3.Count);
			foreach( DataItem i in v3.lst )
			{
				sv.Write(i.X);
				sv.Write(i.Y);
				sv.Write(i.E.X);
				sv.Write(i.E.X);
			}
			sv.Close();
		}
		catch(FileNotFoundException)
		{
			Console.WriteLine( "Err: file "+filename+" not found\n" );
			return false;
		}
		finally
		{
			if( fs != null )
			{
				fs.Close();
			}
		}
		return true;
	}
	public static bool LoadAsBinary( string filename, ref V3DataList v3 )
	{
		FileStream fs = null;
		try
		{
			fs = new FileStream(filename, FileMode.Open );
			BinaryReader lf = new BinaryReader(fs);
			v3.name = lf.ReadString();
			v3.dttm = DateTime.FromBinary(lf.ReadInt64());
			int count = lf.ReadInt32();
			for( int i = 0; i < count; i++)
			{
				DataItem v = new DataItem( lf.ReadDouble(), lf.ReadDouble(), 
						new Vector2( lf.ReadSingle(),
						       	lf.ReadSingle()));
				v3.Add(v);
			}
			lf.Close();
		}
		catch(FileNotFoundException)
		{
			Console.WriteLine( "Err: file "+filename+" not found\n" );
			return false;
		}
		finally
		{
			if( fs != null )
			{
				fs.Close();
			}
		}
		return true;
	}



}

class V3DataArray: V3Data
{
	public int Ox { get; private set; }
	public int Oy { get; private set; }
	public double dx { get; private set; }
	public double dy { get; private set; }
	public Vector2[,] Arr { get; private set; } 
	public override int Count { get { return Ox*Oy; } }
	public override double MaxDistance 
	{ 
		get
		{
			if( Ox == 0 || Oy == 0 )
		       	{
				return 0;
			}
			float a = Convert.ToSingle((Ox-1)*dx);
			float b = Convert.ToSingle((Oy-1)*dy);
			return ( new Vector2(a,b).Length() );
		}
	}
	public V3DataArray( string str, DateTime date ):base(str, date)
	{
		Arr = new Vector2[0,0];
	}
	public V3DataArray( string str, DateTime dt, int a, int b, double x,
			double y, FdblVector2 F):base( str, dt)
	{
		this.dx = x;
		this.dy = y;
		this.Ox = a;
		this.Oy = b;
		Arr = new Vector2[Ox,Oy];
		for( int i = 0; i < Ox; i++ )
		{
			for( int n = 0; n < Oy; n++ )
			{
				Arr[i,n] = F( i*dx, n*dy );
			}
		}
	}
	public override string ToString()
	{
		return String.Format("V3DataArray,{0},{1},{2},{3},{4},{5}\n",
				base.name,base.dttm,this.Ox,
					this.Oy,this.dx,this.dy);
	}
	public override string ToLongString( string format )
	{
		string str_out = "";
		str_out += this.ToString();
		for( int i = 0; i < Ox; i++ )
		{
			for( int n = 0; n < Oy; n++ )
			{
				float a = Convert.ToSingle( Arr[i,n].X );
				float b = Convert.ToSingle( Arr[i,n].Y );
				Vector2 vec = new Vector2(a,b);
				str_out += " x=" + String.Format(format,i*dx);
				str_out += " y=" + String.Format(format,n*dy);
				str_out += " Ex=" + String.Format(format,vec.X);
				str_out += " Ey=" + String.Format(format,vec.Y);
				str_out += " |E|=" + String.Format(format,vec.Length());
				str_out += "\n";
			}
		}
		return str_out;
	}
	public V3DataList ConvertToV3DataList()
	{
		bool b;
		V3DataList p = new V3DataList( base.name, base.dttm );
		for( int i = 0; i < Ox; i++ )
		{
			for( int n = 0; n < Oy; n++ )
			{
				DataItem v = new DataItem(i*dx, n*dy, Arr[i,n]);
				b = p.Add(v);
			}
		}
		return p;
	}
	public override IEnumerator<DataItem> GetEnumerator()
	{ 	
		V3DataList List = this.ConvertToV3DataList();
		return List.GetEnumerator();
       	}
	public static bool SaveAsText( string filename, V3DataArray v3 )
	{
		FileStream fs = null;
		try
		{
			fs = new FileStream( filename, FileMode.Open );
			StreamWriter sw = new StreamWriter(fs);
			sw.WriteLine(v3.name);
			sw.WriteLine(v3.dttm);
			sw.WriteLine(v3.Ox);
			sw.WriteLine(v3.Oy);
			sw.WriteLine(v3.dx);
			sw.WriteLine(v3.dy);
			foreach ( var vec in v3.Arr )
			{
				sw.WriteLine(vec.X);
				sw.WriteLine(vec.Y);
			}
			sw.Close();
		}
		catch(FileNotFoundException)
		{
			Console.WriteLine( "Err: file "+filename+" not found\n" );
			return false;
		}
		finally
		{
			if(fs != null)
			{
				fs.Close();
			}
		}
		return true;
	}
	public static bool LoadAsText( string filename, ref V3DataArray v3 )
	{
		FileStream fs = null;
		try
		{
			fs = new FileStream( filename, FileMode.Open );
			StreamReader sr = new StreamReader(fs);
			v3.name = sr.ReadLine();
			v3.dttm = DateTime.Parse(sr.ReadLine());
			v3.Ox = int.Parse(sr.ReadLine());
			v3.Oy = int.Parse(sr.ReadLine());
			v3.dx = double.Parse(sr.ReadLine());
			v3.dy = double.Parse(sr.ReadLine());
			v3.Arr = new Vector2[v3.Ox,v3.Oy];
			for( int i = 0; i < v3.Ox; i++ )
			{
				for( int j = 0; j < v3.Oy; j++ )
				{
					v3.Arr[i,j].X = float.Parse(sr.ReadLine());
					v3.Arr[i,j].Y = float.Parse(sr.ReadLine());
				}
			}
			sr.Close();
		}
		catch(FileNotFoundException)
		{
			Console.WriteLine( "Err: file "+filename+" not found\n" );
			return false;
		}
		finally
		{
			if(fs != null)
			{
				fs.Close();
			}
		}
		return true;
	}
}

class V3MainCollection
{
	private List<V3Data> Lst;
	public V3MainCollection()
	{
		Lst = new List<V3Data>();
	}
	public V3Data this[int index]
	{
		get { return Lst[index]; }
		set { Lst[index] = value; }
	}
	public int Count { get { return Lst.Count; } }
	public Nullable<DataItem> MaxDistData
	{
		get 
		{
			if( Lst.Count == 0 )
			{
				return null;
			}	
			var DataItems = from i in Lst from j in i select j;
			if( DataItems.Count() == 0 )
			{
				return null;
			}
			double  max_dist= (DataItems.Select( i => i.X*i.X + i.Y*i.Y ).Max());
			var MaxDistItems = from i in DataItems
					where i.X*i.X + i.Y*i.Y == max_dist
					select i;
			return MaxDistItems.Last();
		}
	}
	public IEnumerable<double> XCordMoreThanOnce
	{
		get
		{
			if( this.Lst.Count == 0 )
			{
				return null;
			}
			var Items = from i in Lst from j in i select j;
			var XcordSet = from j in Items select j.X;
			var XGroup = from i in Items 
					where ( XcordSet.Count( j => j==i.X ) > 1 )
					select i.X;
			XGroup = XGroup.Distinct();
			if( XGroup.Count() == 0 )
			{
				return null;
			}
			return XGroup;	
		}
	}
	public IEnumerable<V3Data> EarliestDate
	{
		get
		{
		if( this.Lst.Count == 0 )
		{
			return null;
		}
		var V3DataSet = from i in Lst
		       	where i.dttm == ( from j in Lst select j.dttm).Min()
			select i;
		return V3DataSet;
		}
	}
	public bool Contains(string ID)
	{
		foreach( V3Data p in Lst )
		{
			if( p.name == ID )
			{
				return true;
			}
		}
		return false;
	}
	public bool Add( V3Data v3Data )
	{
		if( this.Contains( v3Data.name ) )
		{
			return false;
		}
		Lst.Add( v3Data );
		return true;
	}
	public string ToLongString( string format )
	{
		string str = "";
		foreach( V3Data p in Lst )
		{
			str += p.ToLongString( format ) + '\n';
		}
		return str ;
	}
	public override string ToString()
	{
		string str = "";
		foreach( V3Data p in Lst )
		{
			str += p.ToString();
		}
		return str + "\n";
	}
}

class Programm
{
	static void SaveLoadTest()
	{
		int b;
		FdblVector2 G;
		G = F;
		DateTime date = new DateTime();
		V3DataArray DataArrToSave = new V3DataArray("Pigeon", date,2,2,0.1,0.1,G);
		V3DataArray DataArrToLoad = new V3DataArray("Cuco", date);
		V3DataList ListToSave = new V3DataList("Raptor", date);
		V3DataList ListToLoad = new V3DataList("Woodpecker", date);
		Console.WriteLine("Initial DataArrays\n");
		Console.WriteLine(DataArrToSave.ToLongString("{0}"));
		Console.WriteLine(DataArrToLoad.ToLongString("{0}"));
		Console.WriteLine("Pigeon is saved to file test\n");
		V3DataArray.SaveAsText("test", DataArrToSave);
		Console.WriteLine("Cuco is loaded from file test\n");
		V3DataArray.LoadAsText("test", ref DataArrToLoad );
		Console.WriteLine(DataArrToSave.ToLongString("{0}"));
		Console.WriteLine("Results are as follows\n");
		Console.WriteLine(DataArrToSave.ToLongString("{0}"));
		Console.WriteLine(DataArrToLoad.ToLongString("{0}"));
		Console.WriteLine("Attempting to save to non existing file\n");
		V3DataArray.SaveAsText("NotAFileName", DataArrToSave);
		Console.WriteLine("Attempting to load from non existing file\n");
		V3DataArray.LoadAsText("NotAFileName", ref DataArrToSave);
		b = ListToSave.AddDefaults( 4, G);
		Console.WriteLine("Initial DataLists\n");
		Console.WriteLine(ListToSave.ToLongString("{0}"));
		Console.WriteLine(ListToLoad.ToLongString("{0}"));
		Console.WriteLine("Raptor is saved to file binary_test \n");
		V3DataList.SaveAsBinary( "binary_test", ListToSave );
		Console.WriteLine("Woodpecker is loaded from file binary_test\n");
		V3DataList.LoadAsBinary( "binary_test", ref ListToLoad );
		Console.WriteLine("Results are as follows\n");
		Console.WriteLine(ListToSave.ToLongString("{0}"));
		Console.WriteLine(ListToLoad.ToLongString("{0}"));
		Console.WriteLine("Attempting to save to non existing file\n");
		V3DataList.SaveAsBinary("NotAFileName", ListToSave);
		Console.WriteLine("Attempting to load from non existing file\n");
		V3DataList.LoadAsBinary("NotAFileName", ref ListToSave);
	}
	static void LinQTest()
	{
		FdblVector2 G;
		G = F;
		V3DataArray Arr1 = new V3DataArray ("Array_entry_1", DateTime.Now);
		V3DataArray Arr2 = new V3DataArray("Array_entry_2", new DateTime(),2,2,1,1,G);
		V3DataArray Arr3 = new V3DataArray("Array_entry_3", DateTime.Now,3,3,1,1,G);
		V3DataList List1 = new V3DataList("List_entry_1", DateTime.Now);
		V3DataList List2 = new V3DataList("List_entry_2", new DateTime());
		V3DataList List3 = new V3DataList("List_entry_3", DateTime.Now);
		List3.AddDefaults( 4, G);
		List2.AddDefaults( 5, G);
		V3MainCollection EmptyColl = new V3MainCollection();
		V3MainCollection TestColl = new V3MainCollection();
		TestColl.Add(Arr1);
		TestColl.Add(Arr2);
		TestColl.Add(Arr3);
		TestColl.Add(List1);
		TestColl.Add(List2);
		TestColl.Add(List3);
		Console.WriteLine("Non-empty collection contains:\n");
		Console.WriteLine(TestColl.ToLongString("{0}"));
		Console.WriteLine("Test for most distant item\n");
		Console.WriteLine(TestColl.MaxDistData);
		Console.WriteLine("Test for most distant item in empry collection\n");
		if( EmptyColl.MaxDistData != null )
		{
			Console.WriteLine("BAD");
		} else {
			Console.WriteLine("null\n");
		}
		Console.WriteLine("Test for X coord more than once enumerable\n");
		IEnumerable<double> TestEnumDouble = TestColl.XCordMoreThanOnce;
		foreach( var i in TestEnumDouble )
		{
			Console.WriteLine(i);
		}		
		Console.WriteLine("\nTest for X coord more than once enumerable(Empty)\n");
		if( EmptyColl.XCordMoreThanOnce != null )
		{
			Console.WriteLine("BAD");
		} else {
			Console.WriteLine("null\n");
		}
		Console.WriteLine("Test for earliest date  enumerable\n");
		IEnumerable<V3Data> TestEnumData = TestColl.EarliestDate;
		foreach( var i in TestEnumData )
		{
			Console.WriteLine(i);
		}		
		Console.WriteLine("\nTest for earliest date enumerable(Empty)\n");
		if( EmptyColl.XCordMoreThanOnce != null )
		{
			Console.WriteLine("BAD");
		} else {
			Console.WriteLine("null\n");
		}
	}		
	static void Main()
	{
		LinQTest();
		SaveLoadTest();
	}
	private static Vector2 F(  double x, double y )
	{
		float a = Convert.ToSingle(x);
		float b = Convert.ToSingle(y);
		return new Vector2( a, b);
	}
}

