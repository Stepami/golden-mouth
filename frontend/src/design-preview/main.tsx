import {createRoot} from 'react-dom/client';
import {useState} from 'react';
import BackendApp from '../v7/BackendApp';
import VisualReview from '../v7/review/VisualReview';
import {ErrorBoundary} from '../app/ErrorBoundary';
import {installPreviewApi,resetPreview,setPreviewTurn,type PreviewTurn} from './api';
import '../v7/theme.css';
import '../v7/ui/ui.css';
import '../v7/app.css';
import '../v7/integration.css';
import '../v7/world-entry.css';
import '../v7/night.css';
import './preview.css';

installPreviewApi();
function DesignPreview(){
 const [gallery,setGallery]=useState(false);
 return <main className="design-preview-layout"><header className="design-preview-toolbar"><span>Реки · <strong>дизайн-превью</strong><small>Демоданные · без AI и сервера</small></span><nav><label className="preview-turn-picker">Пример хода<select aria-label="Пример результата хода" defaultValue="partial" onChange={e=>setPreviewTurn(e.target.value as PreviewTurn)}><option value="partial">Частичный прогресс</option><option value="unchanged">Подписчики, без роста навыков</option><option value="success">Задание выполнено</option><option value="loss">Ушло 8 подписчиков</option></select></label><button aria-pressed={!gallery} onClick={()=>setGallery(false)}>Флоу</button><button aria-pressed={gallery} onClick={()=>setGallery(true)}>Компоненты</button><button onClick={resetPreview}>Начать заново</button></nav></header><div className="design-preview-content">{gallery?<VisualReview/>:<BackendApp/>}</div></main>;
}
createRoot(document.getElementById('root')!).render(<ErrorBoundary><DesignPreview/></ErrorBoundary>);
