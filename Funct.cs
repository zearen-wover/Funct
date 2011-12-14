using System;
using System.Text;

namespace Funct {
	public class FunctEvalError : Exception {
		public FunctEvalError() : base() {}
		public FunctEvalError(string str) : base(str) {}
	}
	
	public abstract class Funct {
		public Funct () {}
		
		public abstract double eval(double x);
	}
	
	public class Lambda : Funct {
		public override double eval(double x) {
			throw new FunctEvalError("Evaluation of Funct failed, uninitialized child funct");
		}
	}
	
	public class Constant : Funct {
		private double _val;
		
		public Constant(double val=0) {
			_val = val;
		}
		
		public override double eval(double x) {
			return _val;
		}
		
		public override string ToString() {
			return _val.ToString();
		}
	}
	
	public class Variable : Funct {
		private string _name;
		
		public string Name {
			get {
				return _name;
			}
		}
		
		public Variable(string name = "x") {
			_name = name;
		}
		
		public override double eval(double x) {
			return x;
		}
		
		public override string ToString() {
			return _name;
		}
	}
	
	public class Unary : Funct {
		public delegate double Function(double x);
		
		private Function _f;
		private static uint _fCount = 1;
		private string _name; 
			
		public Funct Inner{get; set;}
		
		public string Name{
			get {
				return _name;
			}
		}
		
		public Unary(Function f, string name = "") {
			_f = f;
			Inner = new Lambda();
			if (name == "") {
				_name = "f" + _fCount.ToString();
				_fCount++;
			}
			else {
				_name = name;
			}
		}
		
		public override double eval(double x) {
			return _f(Inner.eval(x));
		}
		
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(_name).Append('(').Append(Inner.ToString()).Append(')');
			return sb.ToString();
		}
	}
	
	public class Binary : Funct {
		public delegate double Op(double left, double right);
		
		private Op _op;
		private static uint _opCount = 1;
		private string _symbol;
		
		public Funct Left{get; set;}
		public Funct Right{get; set;}
		
		public Binary(Op op, string symbol = "") {
			_op = op;
			Left = new Lambda();
			Right = new Lambda();
			if (symbol == "") {
				StringBuilder sb = new StringBuilder();
				sb.Append('[').Append(_opCount.ToString()).Append(']');
				_symbol = sb.ToString();
				_opCount++;
			}
			else {
				_symbol = symbol;
			}
		}
		
		public override double eval(double x) {
			return _op(Left.eval(x), Right.eval(x));
		}
		
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append('(').Append(Left.ToString());
			sb.Append(_symbol);
			sb.Append(Right.ToString()).Append(')');
			return sb.ToString();
		}
	}
}