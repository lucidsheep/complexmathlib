using UnityEditor;
using UnityEngine;

namespace ComplexMathLib
{
    [System.Serializable]
    public struct ComplexNumber
    {
        public float r;
        public float i;


        public ComplexNumber(float realValue)
        {
            r = realValue;
            i = 0f;
        }

        public ComplexNumber(float realValue, float imaginaryValue)
        {
            r = realValue;
            i = imaginaryValue;
        }

        public override string ToString()
        {
            if (i == 0f) return r.ToString();
            if (r == 0f) return i + "i";
            return r + (i < 0f ? " - " + (i * -1) : " + " + i) + "i";
        }

        public static ComplexNumber NormalizeLinear(ComplexNumber input)
        {
            var r = input.r;
            var i = input.i;
            while (r < -1f)
                r += 2f;
            while (r > 1f)
                r -= 2f;
            while (i < -1f)
                i += 2f;
            while (i > 1f)
                i += 2f;

            return new ComplexNumber(r, i);
        }

        public ComplexNumber NormalizeLinear()
        {
            return NormalizeLinear(this);
        }
        public static ComplexNumber NormalizeScalar(ComplexNumber input, float limit = 1f)
        {
            float rAbs = Mathf.Abs(input.r);
            float iAbs = Mathf.Abs(input.i);
            float larger = rAbs > iAbs ? rAbs : iAbs;
            if(larger > limit)
            {
                float scale = limit / larger;
                return new ComplexNumber(input.r * scale, input.i * scale);
            }
            return input;
        }
        public ComplexNumber NormalizeScalar(float limit = 1f)
        {
            return NormalizeScalar(this, limit);
        }
        public static implicit operator float(ComplexNumber d) => d.r;
        public static implicit operator ComplexNumber(float f) => new ComplexNumber(f);

        public static ComplexNumber operator + (ComplexNumber lhs, ComplexNumber rhs)
        {
            return new ComplexNumber(lhs.r + rhs.r, lhs.i + rhs.i);
        }
        public static ComplexNumber operator - (ComplexNumber lhs, ComplexNumber rhs)
        {
            return new ComplexNumber(lhs.r - rhs.r, lhs.i - rhs.i);
        }
        public static ComplexNumber operator * (ComplexNumber lhs, ComplexNumber rhs)
        {
            return new ComplexNumber((lhs.r * rhs.r) - (lhs.i * rhs.i), (lhs.r * rhs.i)+(lhs.i * rhs.r));
        }
        public static ComplexNumber operator / (ComplexNumber lhs, ComplexNumber rhs)
        {
            var denom = (rhs.r * rhs.r) + (rhs.i * rhs.i);
            if (denom == 0f)
                denom = 0.000001f; //avoid crashes, should really be NaN
            return new ComplexNumber(((lhs.r * rhs.r) + (lhs.i * rhs.i)) / denom, ((lhs.i *rhs.r)-(lhs.r*rhs.i))/denom);
        }

        public static bool operator == (ComplexNumber lhs, ComplexNumber rhs)
        {
            return lhs.r == rhs.r && lhs.i == rhs.i;
        }

        public static bool operator !=(ComplexNumber lhs, ComplexNumber rhs)
        {
            return lhs.r != rhs.r || lhs.i != rhs.i;
        }
        public ComplexNumber Pow(int exponent)
        {
            var ret = new ComplexNumber(1f);
            for(int n = 0; n < exponent; n++)
            {
                ret = ret * this;
            }
            return ret;
        }
        public static ComplexNumber Pow(int exponent, ComplexNumber val)
        {
            return val.Pow(exponent);
        }

        public override bool Equals(object obj)
        {
            if (obj is ComplexNumber)
                return this == (ComplexNumber)obj;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ i.GetHashCode();
        }

#if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(ComplexNumber))]
        public class ComplexNumberDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2);
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                // Find the SerializedProperties by name
                var x = property.FindPropertyRelative(nameof(r));
                var y = property.FindPropertyRelative(nameof(i));
                float[] floats = { x.floatValue , y.floatValue };
                EditorGUI.MultiFloatField(position, label, new[] { new GUIContent("r"), new GUIContent("i") }, floats);

                x.floatValue = floats[0];
                y.floatValue = floats[1];

            }

        }

#endif

    }

}
