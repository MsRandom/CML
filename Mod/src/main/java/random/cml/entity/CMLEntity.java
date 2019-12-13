package random.cml.entity;

import net.minecraft.entity.AgeableEntity;
import net.minecraft.entity.EntityType;
import net.minecraft.entity.ai.goal.FollowOwnerGoal;
import net.minecraft.entity.passive.TameableEntity;
import net.minecraft.world.World;

import javax.annotation.Nullable;

public class CMLEntity extends TameableEntity {

    public final CMLEntityProperties properties;

    protected CMLEntity(EntityType<? extends CMLEntity> type, World worldIn, CMLEntityProperties properties) {
        super(type, worldIn);
        this.properties = properties;
    }

    @Override
    protected void registerAttributes() {
        super.registerAttributes();
    }

    @Override
    protected void registerGoals() {
        super.registerGoals();
        this.goalSelector.addGoal(0, new FollowOwnerGoal(this, properties.generalSpeed, 4, 16));
    }

    @Nullable
    @Override
    public AgeableEntity createChild(AgeableEntity ageable) {
        return null;
    }

    private boolean hasElement(Element element) {
        return (properties.elements & element.id) == element.id;
    }
}
