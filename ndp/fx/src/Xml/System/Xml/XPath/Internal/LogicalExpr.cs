//------------------------------------------------------------------------------
// <copyright file="LogicalExpr.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace MS.Internal.Xml.XPath {
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml.Xsl;

    internal sealed class LogicalExpr : ValueQuery {
        Operator.Op op;
        Query opnd1;
        Query opnd2;
        
        public LogicalExpr(Operator.Op op, Query  opnd1, Query  opnd2) {
            Debug.Assert(
                Operator.Op.LT == op || Operator.Op.GT == op ||
                Operator.Op.LE == op || Operator.Op.GE == op ||
                Operator.Op.EQ == op || Operator.Op.NE == op
            );
            this.op    = op;
            this.opnd1 = opnd1;
            this.opnd2 = opnd2;
        }
        private LogicalExpr(LogicalExpr other) : base (other) {
            this.op    = other.op;
            this.opnd1 = Clone(other.opnd1);
            this.opnd2 = Clone(other.opnd2);
        }

        public override void SetXsltContext(XsltContext context){
            opnd1.SetXsltContext(context);
            opnd2.SetXsltContext(context);
        }

        public override object Evaluate(XPathNodeIterator nodeIterator) {
            Operator.Op op = this.op;
            object val1 = this.opnd1.Evaluate(nodeIterator);
            object val2 = this.opnd2.Evaluate(nodeIterator);
            int type1 = (int)GetXPathType(val1);
            int type2 = (int)GetXPathType(val2);
            if (type1 < type2) {
                op = Operator.InvertOperator(op);
                object valTemp = val1;
                val1 = val2;
                val2 = valTemp;
                int typeTmp = type1;
                type1 = type2;
                type2 = typeTmp;
            }

            if (op == Operator.Op.EQ || op == Operator.Op.NE) {
                return CompXsltE[type1][type2](op, val1, val2);
            } else {
                return CompXsltO[type1][type2](op, val1, val2);
            }
        }

        delegate bool cmpXslt(Operator.Op op, object val1, object val2);

        //                              Number,                       String,                        Boolean,                     NodeSet,                      Navigator
        private static readonly cmpXslt[][] CompXsltE = {                                                                                         
            new cmpXslt[] { new cmpXslt(cmpNumberNumber), null                         , null                       , null                        , null                    }, 
            new cmpXslt[] { new cmpXslt(cmpStringNumber), new cmpXslt(cmpStringStringE), null                       , null                        , null                    }, 
            new cmpXslt[] { new cmpXslt(cmpBoolNumberE ), new cmpXslt(cmpBoolStringE  ), new cmpXslt(cmpBoolBoolE  ), null                        , null                    }, 
            new cmpXslt[] { new cmpXslt(cmpQueryNumber ), new cmpXslt(cmpQueryStringE ), new cmpXslt(cmpQueryBoolE ), new cmpXslt(cmpQueryQueryE ), null                    },
            new cmpXslt[] { new cmpXslt(cmpRtfNumber   ), new cmpXslt(cmpRtfStringE   ), new cmpXslt(cmpRtfBoolE   ), new cmpXslt(cmpRtfQueryE   ), new cmpXslt(cmpRtfRtfE) },
        };
        private static readonly cmpXslt[][] CompXsltO = {                                                                                         
            new cmpXslt[] { new cmpXslt(cmpNumberNumber), null                         , null                       , null                        , null                    }, 
            new cmpXslt[] { new cmpXslt(cmpStringNumber), new cmpXslt(cmpStringStringO), null                       , null                        , null                    }, 
            new cmpXslt[] { new cmpXslt(cmpBoolNumberO ), new cmpXslt(cmpBoolStringO  ), new cmpXslt(cmpBoolBoolO  ), null                        , null                    }, 
            new cmpXslt[] { new cmpXslt(cmpQueryNumber ), new cmpXslt(cmpQueryStringO ), new cmpXslt(cmpQueryBoolO ), new cmpXslt(cmpQueryQueryO ), null                    },
            new cmpXslt[] { new cmpXslt(cmpRtfNumber   ), new cmpXslt(cmpRtfStringO   ), new cmpXslt(cmpRtfBoolO   ), new cmpXslt(cmpRtfQueryO   ), new cmpXslt(cmpRtfRtfO) },
        };
        
        /*cmpXslt:*/
        static bool cmpQueryQueryE(Operator.Op op, object val1, object val2) {
            Debug.Assert(op == Operator.Op.EQ || op == Operator.Op.NE);
            bool isEQ = (op == Operator.Op.EQ);

            NodeSet n1 = new NodeSet(val1);
            NodeSet n2 = new NodeSet(val2);

            while (true) {
                if (! n1.MoveNext()) {
                    return false;
                }
                if (! n2.MoveNext()) {
                    return false;
                }

                string str1 = n1.Value;

                do {
                    if ((str1 == n2.Value) == isEQ) {
                        return true;
                    }
                }while (n2.MoveNext());
                n2.Reset();    
            }
        }
        
        /*cmpXslt:*/
        static bool cmpQueryQueryO(Operator.Op op, object val1, object val2) {
            Debug.Assert(
                op == Operator.Op.LT || op == Operator.Op.GT ||
                op == Operator.Op.LE || op == Operator.Op.GE
            );

            NodeSet n1 = new NodeSet(val1);
            NodeSet n2 = new NodeSet(val2);

            while (true) {
                if (!n1.MoveNext()) {
                    return false;
                }
                if (!n2.MoveNext()) {
                    return false;
                }

                double num1 = NumberFunctions.Number(n1.Value);

                do {
                    if (cmpNumberNumber(op, num1, NumberFunctions.Number(n2.Value))) {
                        return true;
                    }
                } while (n2.MoveNext());
                n2.Reset();
            }
        }        
        static bool cmpQueryNumber(Operator.Op op, object val1, object val2) {
            NodeSet n1 = new NodeSet(val1);
            double n2 = (double) val2;

            while (n1.MoveNext()) {
                if (cmpNumberNumber(op, NumberFunctions.Number(n1.Value), n2)) {
                    return true;
                }
            }
            return false;
        }
        
        static bool cmpQueryStringE(Operator.Op op, object val1, object val2) {
            NodeSet n1 = new NodeSet(val1);
            string n2 = (string) val2;

            while (n1.MoveNext()) {
                if (cmpStringStringE(op, n1.Value, n2)) {
                    return true;
                }
            }
            return false;
        }

        static bool cmpQueryStringO(Operator.Op op, object val1, object val2) {
            NodeSet n1 = new NodeSet(val1); 
            double n2 = NumberFunctions.Number((string) val2);

            while (n1.MoveNext()) {
                if (cmpNumberNumberO(op, NumberFunctions.Number(n1.Value), n2)) {
                    return true;
                }
            }
            return false;
        }

        static bool cmpRtfQueryE(Operator.Op op, object val1, object val2) {
            string n1 = Rtf(val1);
            NodeSet n2 = new NodeSet(val2);

            while (n2.MoveNext()) {
                if (cmpStringStringE(op, n1, n2.Value)) {
                    return true;
                }
            }
            return false;
        }

        static bool cmpRtfQueryO(Operator.Op op, object val1, object val2) {
            double n1 = NumberFunctions.Number(Rtf(val1));
            NodeSet n2 = new NodeSet(val2);

            while (n2.MoveNext()) {
                if (cmpNumberNumberO(op, n1, NumberFunctions.Number(n2.Value))) {
                    return true;
                }
            }
            return false;
        }

        static bool cmpQueryBoolE(Operator.Op op, object val1, object val2) {
            NodeSet n1 = new NodeSet(val1);
            bool b1 = n1.MoveNext();
            bool b2 = (bool)val2;
            return cmpBoolBoolE(op, b1, b2);
        }
        
        static bool cmpQueryBoolO(Operator.Op op, object val1, object val2) {
            NodeSet n1 = new NodeSet(val1);
            double d1 = n1.MoveNext() ? 1.0 : 0;
            double d2 = NumberFunctions.Number((bool)val2);
            return cmpNumberNumberO(op, d1, d2);
        }

        static bool cmpBoolBoolE(Operator.Op op, bool n1, bool n2) {
            Debug.Assert( op == Operator.Op.EQ || op == Operator.Op.NE, 
                "Unexpected Operator.op code in cmpBoolBoolE()"
            );
            return (op == Operator.Op.EQ) == (n1 == n2);
        }
        static bool cmpBoolBoolE(Operator.Op op, object val1, object val2) {
            bool n1 = (bool)val1;
            bool n2 = (bool)val2;
            return cmpBoolBoolE(op, n1, n2);
        }

        static bool cmpBoolBoolO(Operator.Op op, object val1, object val2) {
            double n1 = NumberFunctions.Number((bool)val1);
            double n2 = NumberFunctions.Number((bool)val2);
            return cmpNumberNumberO(op, n1, n2);
        }

        static bool cmpBoolNumberE(Operator.Op op, object val1, object val2) {
            bool n1 = (bool)val1;
            bool n2 = BooleanFunctions.toBoolean((double)val2);  
            return cmpBoolBoolE(op, n1, n2);
        }

        static bool cmpBoolNumberO(Operator.Op op, object val1, object val2) {
            double n1 = NumberFunctions.Number((bool)val1);
            double n2 = (double)val2;  
            return cmpNumberNumberO(op, n1, n2);
        }
        
        static bool cmpBoolStringE(Operator.Op op, object val1, object val2) {
            bool n1 = (bool)val1;
            bool n2 = BooleanFunctions.toBoolean((string) val2);  
            return cmpBoolBoolE(op, n1, n2);
        }

        static bool cmpRtfBoolE(Operator.Op op, object val1, object val2) {
            bool n1 = BooleanFunctions.toBoolean(Rtf(val1));
            bool n2 = (bool)val2;
            return cmpBoolBoolE(op, n1, n2);
        }

        static bool cmpBoolStringO(Operator.Op op, object val1, object val2) {
            return cmpNumberNumberO(op, 
                NumberFunctions.Number((bool)val1), 
                NumberFunctions.Number((string) val2)
            );
        }

        static bool cmpRtfBoolO(Operator.Op op, object val1, object val2) {
            return cmpNumberNumberO(op,
                NumberFunctions.Number(Rtf(val1)),
                NumberFunctions.Number((bool)val2)
            );
        }

        static bool cmpNumberNumber(Operator.Op op, double n1, double n2) {
            switch (op) {
            case Operator.Op.LT : return( n1 <  n2 ) ;
            case Operator.Op.GT : return( n1 >  n2 ) ;
            case Operator.Op.LE : return( n1 <= n2 ) ;
            case Operator.Op.GE : return( n1 >= n2 ) ;
            case Operator.Op.EQ : return( n1 == n2 ) ;
            case Operator.Op.NE : return( n1 != n2 ) ;
            }
            Debug.Fail("Unexpected Operator.op code in cmpNumberNumber()");
            return false;
        }
        static bool cmpNumberNumberO(Operator.Op op, double n1, double n2) {
            switch (op) {
            case Operator.Op.LT : return( n1 <  n2 ) ;
            case Operator.Op.GT : return( n1 >  n2 ) ;
            case Operator.Op.LE : return( n1 <= n2 ) ;
            case Operator.Op.GE : return( n1 >= n2 ) ;
            }
            Debug.Fail("Unexpected Operator.op code in cmpNumberNumberO()");
            return false;
        }
        static bool cmpNumberNumber(Operator.Op op, object val1, object val2) {
            double n1 = (double)val1;
            double n2 = (double)val2;
            return cmpNumberNumber(op, n1, n2);
        }
        
        static bool cmpStringNumber(Operator.Op op, object val1, object val2) {
            double n2 = (double)val2;
            double n1 = NumberFunctions.Number((string) val1);  
            return cmpNumberNumber(op, n1, n2);
        }

        static bool cmpRtfNumber(Operator.Op op, object val1, object val2) {
            double n2 = (double)val2;
            double n1 = NumberFunctions.Number(Rtf(val1));
            return cmpNumberNumber(op, n1, n2);
        }

        static bool cmpStringStringE(Operator.Op op, string n1, string n2) {
            Debug.Assert( op == Operator.Op.EQ || op == Operator.Op.NE, 
                "Unexpected Operator.op code in cmpStringStringE()"
            );
            return (op == Operator.Op.EQ) == (n1 == n2);
        } 
        static bool cmpStringStringE(Operator.Op op, object val1, object val2) {
            string n1 = (string) val1;
            string n2 = (string) val2;  
            return cmpStringStringE(op, n1, n2);
        }
        static bool cmpRtfStringE(Operator.Op op, object val1, object val2) {
            string n1 = Rtf(val1);
            string n2 = (string) val2;
            return cmpStringStringE(op, n1, n2);
        }
        static bool cmpRtfRtfE(Operator.Op op, object val1, object val2) {
            string n1 = Rtf(val1);
            string n2 = Rtf(val2);
            return cmpStringStringE(op, n1, n2);
        } 

        static bool cmpStringStringO(Operator.Op op, object val1, object val2) {
            double n1 = NumberFunctions.Number((string) val1);
            double n2 = NumberFunctions.Number((string) val2);
            return cmpNumberNumberO(op, n1, n2);
        }

        static bool cmpRtfStringO(Operator.Op op, object val1, object val2) {
            double n1 = NumberFunctions.Number(Rtf(val1));
            double n2 = NumberFunctions.Number((string)val2);
            return cmpNumberNumberO(op, n1, n2);
        }

        static bool cmpRtfRtfO(Operator.Op op, object val1, object val2) {
            double n1 = NumberFunctions.Number(Rtf(val1));
            double n2 = NumberFunctions.Number(Rtf(val2));
            return cmpNumberNumberO(op, n1, n2);
        }

        public override XPathNodeIterator Clone() { return new LogicalExpr(this); }

        private struct NodeSet {
            private Query opnd;
            private XPathNavigator current;

            public NodeSet(object opnd) {
                this.opnd = (Query) opnd;
                current = null;
            }
            public bool MoveNext() {
                current = opnd.Advance();
                return current != null;
            }

            public void Reset() {
                opnd.Reset();
            }

            public string Value { get { return this.current.Value; } }
        }

        private static string Rtf(    object o) { return ((XPathNavigator)o).Value; }

        public override XPathResultType StaticType { get { return XPathResultType.Boolean; } }

        public override void PrintQuery(XmlWriter w) {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("op", op.ToString());
            opnd1.PrintQuery(w);
            opnd2.PrintQuery(w);
            w.WriteEndElement();
        }
    }
}
