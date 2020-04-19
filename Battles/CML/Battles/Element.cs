using System;

namespace CML.Battles
{
    public enum Element
    {
        Lightning,
        Water,
        Fire,
        Ice,
        Earth,
        Air,
        Light,
        Dark
    }

    public static class Elements
    {
        private static readonly Element[] ElementValues = (Element[]) Enum.GetValues(typeof(Element));

        public static Element[] Values()
        {
            return ElementValues;
        }
        
        public static bool HasAdvantage(this Element current, Element other)
        {
            return other == Element.Lightning && (int) current == Values().Length - 1 || (int) current == (int) other - 1;
        }
    }
}
