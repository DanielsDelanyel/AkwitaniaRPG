// Shader dla URP + 2D Renderer (pasuje do "Sprite-Lit-Default"/"Sprite-Unlit-Default"
// widocznych w Twoim projekcie). Uzywany TYLKO chwilowo, podczas migania po trafieniu
// (patrz Creature.cs -> PlayHitFlash) - poza tym kazde stworzenie normalnie renderuje
// sie swoim wlasnym, docelowym materialem (wiec 2D Lighting dziala jak zawsze).
//
// DLACZEGO TO JEST POTRZEBNE:
// Zwykly material sprite'a (Sprite-Lit-Default, Sprite-Unlit-Default, Sprites/Default...)
// zawsze MNOZY kolor tekstury przez SpriteRenderer.color. Ustawienie tego koloru na
// bialy (1,1,1,1) fizycznie NIC nie zmienia (mnozenie przez 1), dziala tylko czern
// (mnozenie przez 0 = wszystko czarne). Dlatego prawdziwy "flash na bialo" wymaga
// shadera, ktory PODMIENIA kolor (uzywajac tylko ksztaltu/przezroczystosci ze sprite'a),
// zamiast go mnozyc - to robi ten shader przez _FlashColor.
//
// JAK UZYC W EDYTORZE:
//   1. Zaimportuj ten plik do Unity (Assets > Create > ... nie trzeba, samo .shader
//      w folderze Assets wystarczy - Unity go skompiluje automatycznie).
//   2. Stworz NOWY Material (PPM w Project > Create > Material), jako Shader wybierz
//      "Custom/SpriteHitFlash". Nazwij go np. "M_SpriteHitFlash".
//   3. Ten JEDEN material podepnij pod pole "Hit Flash Material" w komponencie
//      Creature - na KAZDYM prefabie przeciwnika/bossa, ktory ma migac. Skrypt sam
//      tworzy sobie tymczasowa kopie przy kazdym trafieniu, wiec jeden wspolny
//      material w zupelnosci wystarczy dla wszystkich stworzen naraz.
Shader "Custom/SpriteHitFlash"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint (zwykle nie ruszaj)", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend One OneMinusSrcAlpha // premultiplied alpha - tak samo jak Sprites/Default
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            half4 _Color;
            half4 _FlashColor;
            half _FlashAmount;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 tinted = texColor * IN.color;

                // Ksztalt/przezroczystosc ZAWSZE ze sprite'a, ale kolor RGB
                // podmieniamy na _FlashColor zamiast go mnozyc - dzieki temu
                // biel faktycznie wychodzi biala, a nie "bez zmian".
                half3 finalRGB = lerp(tinted.rgb, _FlashColor.rgb, _FlashAmount);
                half finalA = tinted.a;

                return half4(finalRGB * finalA, finalA); // premultiplied, jak w Sprites/Default
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
