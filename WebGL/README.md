# LINAC Room Studio — WebGL

Extract the entire archive. With Python installed, run this command from the extracted folder:

```sh
python serve-webgl.py --directory .
```

Open http://127.0.0.1:8080/. Keep the server running while using the app.

For static web hosting, upload the complete folder. Serve `.mjs` and `.js` as `application/javascript`, `.wasm` as `application/wasm`. Open over HTTP/HTTPS rather than double-clicking index.html. Calculation runs locally in a browser worker.

Use **Link walls to room size** to resize walls with the floor and ceiling. Linked wall-height edits update the shared ceiling. Each design has one room/ceiling. Equipment and occupied regions retain their positions.

**Calculate reference QA** remains below the properties panel; results open in a popup with Recalculate, Export QA JSON and Close.

Project source and future development notes: the repository's `unity/PROJECT_HANDOFF.md`.
