import { createRoot } from 'react-dom/client';
import { App } from './App';
import './styles.css';

const root = document.getElementById('root');
const headerControls = document.getElementById('header-controls');
if (!root || !headerControls) throw new Error('Application root is missing.');
createRoot(root).render(<App headerControls={headerControls} />);
