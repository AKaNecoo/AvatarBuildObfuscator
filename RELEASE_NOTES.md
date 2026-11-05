## 中文

- 修复 Thry／Poiyomi 读取混淆 Shader 时 include 路径错误、导致材质锁定失败的问题。
- 缩短 Shader 副本的磁盘文件名，保留内部混淆名称，避免锁定缓存路径过长。

## English

- Fix material locking failures when Thry/Poiyomi reads includes from obfuscated shaders.
- Shorten generated shader filenames while retaining internal obfuscated names to avoid long locker cache paths.

## 日本語

- Thry／Poiyomi が難読化シェーダーの include を読み込む際に、マテリアルのロックが失敗する問題を修正。
- シェーダー内部の難読化名を維持しつつ生成ファイル名を短縮し、ロック用キャッシュのパス長を抑制。
