## 中文

- 修复生成 Shader 的 Thry／Poiyomi 锁定兼容问题。
- 复用 Shader 编译数据、锁定缓存及 Windows 已编码音频，减少重复编译与导入。
- 减少重复的资源引用扫描，保留对 SDK 后期生成资源的处理。

## English

- Fix Thry/Poiyomi locking for generated shaders.
- Reuse compiled shader data, shader lock caches and encoded Windows audio to reduce repeated compilation and imports.
- Reduce repeated resource-reference scans while covering resources added late in SDK processing.

## 日本語

- 生成されたシェーダーの Thry／Poiyomi ロックに関する互換性を修正。
- シェーダーのコンパイル済みデータ、ロックキャッシュ、Windows のエンコード済み音声を再利用し、コンパイルとインポートの重複を削減。
- リソース参照の重複走査を削減し、SDK 処理の後半で追加されたリソースにも対応。
