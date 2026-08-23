# Hand-Drawn Dice (URP 17.5)

Este directorio contiene el sistema visual reutilizable del dado. El shader y los
controladores no dependen de que el mesh sea un D6: la cantidad de caras se obtiene
de los anchors y slots de material encontrados por `DiceVisualController`.

## Uso en este proyecto

1. Instanciar `Prefabs/D6.prefab`.
2. Mantener `DiceVisualController` para texturas, tintes y etiquetas por cara.
3. Mantener `DiceStyleController` para el estilo temporal y la iluminación toon.
4. Mantener `DiceLabelJitterController` para la vibración cuantizada y estable en
   píxeles de las etiquetas.
5. Mantener `DiceRuntimeRenderOptimizer` y `DiceRuntimeLabelOptimizer`: solamente
   actúan en Play Mode y no alteran la vista de escena.
6. Si cambia un Renderer Asset, ejecutar:
   `Tools > Hand-Drawn Dice > Install Outline In All URP Renderers`.

## Adaptar otro tipo de dado

- El mesh puede ser D4, D8, D10, D12, D20 u otra geometría.
- Crear un anchor por cara. Por defecto se buscan nombres `FaceAnchor_XX`.
- Crear un slot de material por cara. Por defecto se buscan nombres que terminen en
  `Face_XX`.
- `Auto Setup Faces` relaciona cada anchor con la geometría por su posición, no por
  el orden de submeshes del FBX, y reordena los materiales de la instancia para que
  `Face_XX` corresponda realmente al label `XX`.
- Mantener Edge como slot independiente cuando el mesh lo proporcione.
- Los prefijos son configurables en `DiceVisualController`.
- Agregar los dos optimizadores al objeto raíz.
- Ejecutar `Auto Setup Faces` y después `Bake Optimized Face Mesh`. El horneado
  crea un asset nuevo; nunca modifica el FBX.
- El camino optimizado admite hasta 32 caras. Esto cubre D4-D20; si un dado excede
  ese límite, conserva automáticamente el renderer original por submeshes.

## Texturas, overlays y fuentes futuras

- `Default Face Texture` es la superficie base compartida por todas las caras. El
  prefab usa `Textures/Marble_Texture.jpg`; se puede reemplazar desde el inspector.
- Si `Default Face Texture` queda vacío, cada cara usa `_BaseMap` de su material y,
  como último fallback, blanco.
- El campo `Texture` de cada cara es ahora un overlay opcional. Su alpha se compone
  sobre la base y debajo del número; `None` significa que esa cara no tiene overlay.
- Para overlays usar PNG con transparencia, como `Fire_Texture.png` e
  `Ice_Texture.png`. Un JPG no puede conservar las zonas transparentes.
- `Face Tint` multiplica la composición base + overlay. Las caras nuevas y el D6 se
  inicializan en `#FAFAFA` como override casi blanco; `Color.white` sigue heredando
  el color del material por compatibilidad.
- En Edit Mode los cambios se previsualizan inmediatamente. En Play Mode se acumulan
  hasta usar uno de los botones `Apply`, para no reconstruir recursos varias veces
  durante una pausa.
- Edge utiliza el mismo shader, pero conserva material, textura y tinte propios.
- `Default Label Font` utiliza `Fonts/Neythal-Regular SDF.asset`, generado desde
  la fuente original incluida en el paquete.
- Cada cara puede reemplazar la fuente y el texto de forma independiente.
- El jitter de labels modifica únicamente el contorno SDF del glifo. No mueve ni
  rota el `Transform`, por lo que el número permanece estable al mirar la cara en
  ángulos rasantes.
- El efecto funciona con cualquier cantidad de caras porque busca los labels por
  nombre, no valores específicos de D6. Una fuente futura necesita un material
  compatible con `Dice/Hand Drawn Text SDF` para conservar el mismo line boil.

## Cambios durante una pausa

Los setters no reconstruyen recursos por defecto. Se pueden acumular todos los
cambios de una pausa y confirmarlos una sola vez:

```csharp
visuals.SetFaceOverlayTexture("01", newOverlayPng);
visuals.SetFaceTint("01", Color.red);
visuals.SetFaceValue("02", 20);
visuals.SetFaceCustomLabel("03", "★");

visuals.ApplyFaceAppearanceChanges();
visuals.ApplyLabelChanges();
```

También se puede pasar `applyImmediately: true` a un setter para una actualización
aislada. En el inspector, el botón `Apply Face Overlay / Tint Changes` hace lo mismo.

## Optimización runtime

- Las caras se hornean en un solo submesh y usan arrays separados para bases y
  overlays; Edge conserva su propio submesh/material. Un D6 o D20 pasa de una llamada
  por superficie a dos.
- Dados que usan el mismo conjunto de bases u overlays comparten esos arrays en memoria.
- Los labels con el mismo material/fuente se combinan en un renderer por dado. Si
  se mezclan fuentes, se crea un renderer por material de fuente.
- No hay `Update` ni trabajo de CPU por frame en estos controladores. El movimiento
  del dado no reconstruye meshes, texturas, property blocks ni labels.
- El costo de crear el array y recombinar texto ocurre al entrar en Play Mode o al
  llamar explícitamente a los métodos de aplicación durante una pausa.

## Exportación

### Crear el archivo `.unitypackage`

1. Cerrar Play Mode y guardar el prefab.
2. En Project, seleccionar únicamente la carpeta `Assets/Dice`.
3. Usar `Assets > Export Package...`.
4. Activar `Include dependencies` y luego `Export`.
5. No agregar `Library`, `Temp`, `Logs`, `ProjectSettings`, la escena de prueba ni
   los Renderer Assets de este proyecto. El instalador configura los Renderer Assets
   propios del proyecto destino.

### Instalarlo en otro proyecto

1. Usar preferentemente Unity 6.5 con URP 17.5. Otras versiones de URP pueden requerir
   adaptar la API de `FullScreenPassRendererFeature`.
2. Instalar Universal RP y TextMeshPro. Ejecutar
   `Window > TextMeshPro > Import TMP Essential Resources` antes de importar el dado;
   el shader de texto reutiliza `TMPro_Properties.cginc`.
3. Importar el `.unitypackage` con todos sus archivos.
4. Ejecutar
   `Tools > Hand-Drawn Dice > Install Outline In All URP Renderers`.
5. Confirmar que la cámara usa uno de esos Universal Renderer Assets e instanciar
   `Assets/Dice/Prefabs/D6.prefab`.

El outline utiliza el bit de stencil de usuario con valor 8. Los materiales exponen
propiedades ocultas para cambiarlo si el proyecto destino ya reserva ese bit. Para un
dado nuevo, ejecutar `Auto Setup Faces` y después `Bake Optimized Face Mesh`; incluir
en la siguiente exportación el prefab, el mesh fuente y el asset horneado resultante.
