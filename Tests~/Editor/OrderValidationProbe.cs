// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(AvatarTools.Obfuscation.Validation.OrderValidationProbe))]

namespace AvatarTools.Obfuscation.Validation
{
    // Opt-in test instrumentation only. Records component identities after upstream
    // optimizers and before obfuscation, so comparisons never assume sibling order.
    public sealed class OrderValidationProbe : Plugin<OrderValidationProbe>
    {
        public override string QualifiedName => "dev.avatar-tools.obfuscation.validation-order-probe";
        public override string DisplayName => "Obfuscator validation probe (test only)";
        public static bool Enabled;
        public static Renderer[] Renderers;
        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("com.anatawa12.avatar-optimizer")
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("com.vrcfury.vrcfury")
                .AfterPlugin("net.rs64.tex-trans-tool")
                .AfterPlugin("dev.cocokoishi.avatar-obfuscator")
                .BeforePlugin("dev.avatar-tools.build-obfuscator")
                .Run("Capture renderer identity for order-independent validation", context => {
                    if (Enabled) Renderers = context.AvatarRootObject.GetComponentsInChildren<Renderer>(true);
                });
        }
    }
}
