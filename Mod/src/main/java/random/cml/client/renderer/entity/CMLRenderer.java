package random.cml.client.renderer.entity;

import net.minecraft.client.renderer.entity.EntityRendererManager;
import net.minecraft.client.renderer.entity.MobRenderer;
import net.minecraft.client.renderer.entity.model.EntityModel;
import net.minecraft.util.ResourceLocation;
import random.cml.CMLMod;
import random.cml.entity.CMLEntity;

import javax.annotation.Nullable;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;

public class CMLRenderer<T extends EntityModel<CMLEntity>> extends MobRenderer<CMLEntity, T> {
    private static final Map<ResourceLocation, EntityModel<CMLEntity>> MODELS = new HashMap<>();
    private static final Map<ResourceLocation, ResourceLocation> TEXTURES = new HashMap<>();

    public CMLRenderer(EntityRendererManager p_i50961_1_) {
        super(p_i50961_1_, null, 0);
    }

    @SuppressWarnings("unchecked")
    @Override
    public void doRender(CMLEntity entity, double x, double y, double z, float entityYaw, float partialTicks) {
        this.entityModel = (T) MODELS.computeIfAbsent(entity.getType().getRegistryName(), k -> entity.properties.model.get());
        super.doRender(entity, x, y, z, entityYaw, partialTicks);
    }

    @Nullable
    @Override
    protected ResourceLocation getEntityTexture(CMLEntity entity) {
        return TEXTURES.computeIfAbsent(entity.getType().getRegistryName(), k -> new ResourceLocation(CMLMod.MOD_ID, "textures/entity/" + Objects.requireNonNull(k).getPath() + ".png"));
    }
}
