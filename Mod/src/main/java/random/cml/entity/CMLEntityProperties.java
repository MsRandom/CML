package random.cml.entity;

import net.minecraft.client.renderer.entity.model.EntityModel;

import java.util.function.Supplier;

public class CMLEntityProperties {

    public final double defense;
    public final double attack;
    public final double generalSpeed;
    public final double attackSpeed;
    public final boolean canFly;
    public final int followRange;
    public final double knockbackResistance;
    public final Supplier<? extends EntityModel<CMLEntity>> model;
    public final int elements;

    public CMLEntityProperties(double defense, double attack, double generalSpeed, double attackSpeed, boolean canFly, int followRange, double knockbackResistance, Supplier<? extends EntityModel<CMLEntity>> model, Element... elements) {
        this.defense = defense;
        this.attack = attack;
        this.generalSpeed = generalSpeed;
        this.attackSpeed = attackSpeed;
        this.canFly = canFly;
        if (followRange == 0) this.followRange = 16;
        else this.followRange = followRange;
        this.knockbackResistance = knockbackResistance;
        this.model = model;
        int flags = 0;
        for(Element element : elements) flags |= element.id;
        this.elements = flags;
    }
}
