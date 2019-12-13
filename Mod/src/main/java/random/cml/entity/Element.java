package random.cml.entity;

public class Element {

    private static int length = 0;
    public static final Element LIGHTNING = new Element();
    public static final Element WATER = new Element();
    public static final Element FIRE = new Element();
    public static final Element ICE = new Element();
    public static final Element EARTH = new Element();
    public static final Element AIR = new Element();
    public static final Element LIGHT = new Element();
    public static final Element DARK = new Element();
    private int flag;
    public int id;

    public Element() {
        this.flag = length++;
        this.id = 1 << flag;
    }
}
