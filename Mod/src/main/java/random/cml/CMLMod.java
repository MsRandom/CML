package random.cml;

import net.minecraft.entity.EntityType;
import net.minecraftforge.client.event.ModelRegistryEvent;
import net.minecraftforge.event.RegistryEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.client.registry.RenderingRegistry;
import net.minecraftforge.fml.common.Mod;
import random.cml.client.renderer.entity.CMLRenderer;
import random.cml.entity.CMLEntity;

@Mod(CMLMod.MOD_ID)
public class CMLMod
{
    public static final String MOD_ID = "cml";

    @Mod.EventBusSubscriber(bus=Mod.EventBusSubscriber.Bus.MOD)
    public static class RegistryEvents {
        @SubscribeEvent
        public static void registerEntities(final RegistryEvent.Register<EntityType<?>> event) {
            //register entities here
        }

        @SubscribeEvent
        public static void registerRenders(final ModelRegistryEvent event) {
            RenderingRegistry.registerEntityRenderingHandler(CMLEntity.class, CMLRenderer::new);
        }
    }
}
