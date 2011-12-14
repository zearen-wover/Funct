using System;
using System.Collections.Generic;

namespace Funct {
	#region Utility classes
	public struct Pair<T1, T2> {
		public Pair(T1 t1, T2 t2) {
			first = t1;
			second = t2;
		}
		public T1 first;
		public T2 second;
	}
	
	public class ParserError : Exception {
		public ParserError() : base("Unkown parser error") {}
		public ParserError(string str) : base(str) {}
	}
	
	public class Paren : Funct {
		public override double eval(double x) {
			try {
				throw new ParserError("Unmatched open parentheses");
			}
			catch (ParserError) {
				throw new FunctEvalError("Not fully parsed");
			}
		}
	}
	#endregion
	
	public class Parser {
		
		#region Initialization
		public enum Precedence {
			subAdd = 0,
			addition = 1,
			addMul = 2,
			multiplication = 3,
			mulExp = 4,
			exponentiation = 5,
			expApp = 6,
			application = 7,
			grouping = 9
		}
		
		public delegate Pair<Precedence, Binary> OpMaker();
		public delegate Unary FunMaker();
		
		private Dictionary<string, OpMaker> _ops;
		private Dictionary<string, FunMaker> _funs;
		
		private string _varName;
		public string VariableName{
			get {
				return _varName;
			}
			set {
				verifyName(value);
				if (_funs.ContainsKey(value))
					throw new ParserError("Invalid variable name: function already exists with that name");
				_varName = value;
			}
		}
		
		public Parser () {
			_funs = new Dictionary<string, FunMaker>();
			_ops = new Dictionary<string, OpMaker>();
			VariableName = "x";
		}
		
		private void verifyName(string name) {
			if (name == "")
				throw new ParserError("Invalid name: empty string");
			foreach (char ch in name) {
				if (!Char.IsLetter(ch))
					throw new ParserError("Invalid name: contains non-alphabetic character");
			}
		}
				
		public void addOp(char symbol, Binary.Op op, Precedence prec) {
			if (Char.IsWhiteSpace(symbol)) {
				throw new ParserError("An operation cannot be whitespace");
			}
			else if (Char.IsDigit(symbol)) {
				throw new ParserError("An operation cannot be a digit");
			}
			else if (Char.IsLetter(symbol)) {
				throw new ParserError("An operation cannot be letter");
			}
			_ops.Add(symbol.ToString(), () => 
				new Pair<Precedence, Binary>(prec, new Binary(op, symbol.ToString ())));
		}
		
		public void removeOp(char symbol) {
			_ops.Remove(symbol.ToString());
		}
		
		public void addFun(string name, Unary.Function f) {
			verifyName(name);
			if (name == VariableName)
				throw new ParserError("Invalid function name: same as variable name");
			_funs.Add(name, () => new Unary(f, name));
		}
		
		public void removeFun(string name) {
			_funs.Remove(name);
		}
		#endregion
		
		#region Tokenization
		private string num(string expr, ref int i) {
			int start = i;
			bool gotDot = false;
			while (i < expr.Length && (Char.IsDigit(expr[i]) || (expr[i] == '.' && !gotDot))) {
				if (expr[i] == '.') gotDot = true;
				i++;
			}
			return expr.Substring(start, i - start);
		}
		
		private string ident(string expr, ref int i) {
			int start = i;
			while (i < expr.Length && Char.IsLetter(expr[i])) i++;
			return expr.Substring(start, i - start);
		}
		
		protected List<string> tokenize(string expr) {
			List<string> tokens = new List<string>();
			int i = 0;
			char cur;
			while (i < expr.Length) {
				cur = expr[i];
				if (Char.IsWhiteSpace(cur)) {
					i++;
				}
				else if (Char.IsDigit(cur) || cur == '.') {
					tokens.Add(num(expr, ref i));
				}
				else if (Char.IsLetter(cur)) {
					tokens.Add(ident(expr, ref i));
				}
				else {
					// We assume it must be an operator
					tokens.Add(cur.ToString());
					i++;
				}
			}
			return tokens;
		}
		#endregion
		
		public Funct parse(string expression) {
			List<string> tokens = tokenize(expression);
			if (tokens.Count == 0)
				throw new ParserError("Empty expression");
			Stack<Pair<Precedence, Funct>> opStack = new Stack<Pair<Precedence, Funct>>();
			Stack<Funct> resStack = new Stack<Funct>();
			bool onOp = false;
			char cur;
			Funct top;
			foreach (string token in tokens) {
				cur = token[0];
				if (onOp) {
					#region On operation
					if (Char.IsDigit(cur) || cur == '.') {
						throw new ParserError(String.Format("Expected operator, but got number: {0}", token));
					}
					else if (Char.IsLetter(cur) || token == "(") {
						throw new ParserError(String.Format("Expected operator, but got function: {0}", token));
					}
					else if (token == ")") {
						#region Close paren
						try {
							do {
								top = opStack.Pop().second;
								if (top is Unary) {
									Unary utop = (Unary) top;
									utop.Inner = resStack.Pop();
									resStack.Push(utop);
								}
								else if (top is Binary) {
									Binary btop = (Binary) top;
									btop.Right = resStack.Pop();
									btop.Left = resStack.Pop();
									resStack.Push(btop);
								}
								else if (top is Paren) {
									break;
								}
							} while (true);
						}
						catch (InvalidOperationException) {
							throw new ParserError("Unmatched closed parentheses");
						}
						#endregion
					}
					else /*(IsOp(token))*/ {
						#region Do operation
						Pair <Precedence, Funct> next = new Pair<Precedence, Funct>(Precedence.subAdd, new Lambda());
						try {
							// Goram C# being stupid about casting.
							Pair<Precedence, Binary> tmp = _ops[token]();
							next.first = tmp.first;
							next.second = tmp.second;
						}
						catch (KeyNotFoundException) {
							throw new ParserError(String.Format("Unrecognized operation: {0}", token));
						}
						if (opStack.Count == 0) {
							opStack.Push(next);
						}
						else {
							Pair<Precedence, Funct> ptop = opStack.Pop();
							if (next.first > ptop.first || ptop.second is Paren) {
								opStack.Push(ptop);
								opStack.Push(next);
							}
							// Add an else if ( == ) {} here to implement associativity
							else {
								Unary utop;
								while (ptop.second is Unary) {
									utop = (Unary) ptop.second;
									utop.Inner = resStack.Pop();
									resStack.Push(utop);
									try {
										ptop = opStack.Pop();
									}
									catch (InvalidOperationException) {
										ptop = new Pair<Precedence,Funct>(Precedence.subAdd, new Lambda());
										break;
									}
								}
								if (ptop.second is Binary) {
									Binary btop = (Binary) ptop.second;
									btop.Right = resStack.Pop ();
									btop.Left = resStack.Pop ();
									resStack.Push(btop);
								}
								opStack.Push(next);
							}
						}
						#endregion
					}
					onOp = false;
					#endregion 
				}
				else {
					#region On unit
					if (Char.IsDigit(cur) || cur == '.') {
						resStack.Push(new Constant(Double.Parse(token)));
						onOp = true;
					}
					else if (token == VariableName) {
						resStack.Push(new Variable(VariableName));
						onOp = true;
					}
					else if (Char.IsLetter(cur)) {
						try {
							opStack.Push(new Pair<Precedence, Funct>(Precedence.application, _funs[token]()));
						}
						catch (KeyNotFoundException) {
							throw new ParserError(String.Format("Unrecognized function: {0}", token));
						}
					}
					else if (token == "-") {
						opStack.Push(new Pair<Precedence, Funct>(Precedence.multiplication, new Unary((x) => -x, " -")));
					}
					else if (token == "(") {
						opStack.Push(new Pair<Precedence, Funct>(Precedence.grouping, new Paren()));
					}
					else {
						throw new ParserError("Expected number, or function, but got operator");
					}
					#endregion
				}
			}
			#region Finish stack
			try {
				while (opStack.Count > 0) {
					top = opStack.Pop().second;
					if (top is Unary) {
						Unary utop = (Unary) top;
						utop.Inner = resStack.Pop();
						resStack.Push(utop);
					}
					else if (top is Binary) {
						Binary btop = (Binary) top;
						btop.Right = resStack.Pop();
						btop.Left = resStack.Pop ();
						resStack.Push (btop);
					}
					else if (top is Paren) {
						throw new ParserError("Unmatched open parentheses");
					}
				}
			}
			catch (InvalidOperationException) {
				throw new ParserError("Too many operators (exhausted result stack)");
			}
			if (resStack.Count != 1) {
				throw new ParserError("Too many numbers (result stack had more than 1 item)");
			}
			#endregion 
			return resStack.Pop();
		}
	}
	
	public class StandardParser : Parser {
		public StandardParser() : base() {
			addOp('+', (left, right) => left + right, Precedence.addition);
			addOp('-', (left, right) => left - right, Precedence.addition);
			addOp('*', (left, right) => left * right, Precedence.multiplication);
			addOp('/', (left, right) => left / right, Precedence.multiplication);
			addOp('^', Math.Pow, Precedence.exponentiation);

            addFun("sqrt", Math.Sqrt);
			addFun("cos", Math.Cos);
			addFun("sin", Math.Sin);
			addFun("tan", Math.Tan);
			addFun("exp", Math.Exp);
			addFun("ln", Math.Log);
		}
	}
}