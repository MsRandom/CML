package random.cml.entity;

import net.minecraft.entity.EntityClassification;
import net.minecraft.entity.EntitySize;
import net.minecraft.entity.EntityType;

public class CmlEntityHandler extends EntityType<CMLEntity> {

    //public static final EntityType<CMLEntity> SWAMP_WALKER = new CmlEntityHandler();

    public CmlEntityHandler(final CMLEntityProperties properties, EntitySize size, String name) {
        super((type, world) -> new CMLEntity(type, world, properties), EntityClassification.CREATURE, false, true, true, true, size, e -> true, e -> properties.followRange, e -> 20, null);
        setRegistryName(name);
    }
}
