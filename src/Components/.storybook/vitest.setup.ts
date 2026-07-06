import { setProjectAnnotations } from '@storybook/react-vite';
import { configure } from 'storybook/test';
import * as previewAnnotations from './preview';

configure({ asyncUtilTimeout: 10_000 });

setProjectAnnotations([previewAnnotations]);