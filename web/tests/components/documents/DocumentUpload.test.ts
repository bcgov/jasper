import DocumentUpload from '@/components/documents/DocumentUpload.vue';
import { shallowMount, type VueWrapper } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

type DocumentUploadVm = {
	showDocumentUpload: boolean;
	selectedUpload: File | null;
	rejectedUploadMessage: string;
	onDocumentSelected: (files: File[] | File | null | undefined) => void;
	onDocumentRejected: (files: File[]) => void;
};

describe('DocumentUpload.vue', () => {
	const createWrapper = (props = {}) =>
		shallowMount(DocumentUpload, {
			props: {
				show: true,
				disabled: false,
				...props,
			},
		});

	const vm = (wrapper: VueWrapper): DocumentUploadVm =>
		wrapper.vm as unknown as DocumentUploadVm;

	describe('Rendering', () => {
		it('renders attach supporting document text', () => {
			const wrapper = createWrapper();

			expect(wrapper.text()).toContain('Attach supporting document (optional)');
		});

		it('applies disabled class when disabled prop is true', () => {
			const wrapper = createWrapper({ disabled: true });

			expect(wrapper.classes()).toContain('upload-disabled');
		});

		it('does not render file upload by default', () => {
			const wrapper = createWrapper();

			expect(wrapper.find('v-file-upload-stub').exists()).toBe(false);
		});
	});

	describe('Watchers', () => {
		it('resets upload state when show becomes false', async () => {
			const wrapper = createWrapper({ show: true });

			vm(wrapper).showDocumentUpload = true;
			vm(wrapper).selectedUpload = new File(['x'], 'selected.pdf', {
				type: 'application/pdf',
			});
			vm(wrapper).rejectedUploadMessage = 'Bad file';

			await wrapper.setProps({ show: false });
			await nextTick();

			expect(vm(wrapper).showDocumentUpload).toBe(false);
			expect(vm(wrapper).selectedUpload).toBe(null);
			expect(vm(wrapper).rejectedUploadMessage).toBe('');
		});

		it('clears rejected upload message when upload area is closed', async () => {
			const wrapper = createWrapper();

			vm(wrapper).showDocumentUpload = true;
			vm(wrapper).rejectedUploadMessage = 'Bad file';
			await nextTick();

			vm(wrapper).showDocumentUpload = false;
			await nextTick();

			expect(vm(wrapper).rejectedUploadMessage).toBe('');
		});

		it('closes upload area when disabled prop becomes true', async () => {
			const wrapper = createWrapper({ disabled: false });

			vm(wrapper).showDocumentUpload = true;
			await wrapper.setProps({ disabled: true });
			await nextTick();

			expect(vm(wrapper).showDocumentUpload).toBe(false);
		});
	});

	describe('onDocumentSelected', () => {
		it('accepts a single file and syncs selectedFile model', async () => {
			const wrapper = createWrapper();
			const uploaded = new File(['x'], 'uploaded.pdf', { type: 'application/pdf' });

			vm(wrapper).onDocumentSelected(uploaded);
			await nextTick();

			expect(vm(wrapper).selectedUpload?.name).toBe('uploaded.pdf');
			expect(vm(wrapper).selectedUpload?.type).toBe('application/pdf');
			expect(wrapper.emitted('update:selectedFile')).toBeTruthy();
			const lastEmit = wrapper.emitted('update:selectedFile')?.at(-1)?.[0] as
				| File
				| undefined;
			expect(lastEmit?.name).toBe('uploaded.pdf');
			expect(lastEmit?.type).toBe('application/pdf');
		});

		it('accepts file arrays and takes the first file', async () => {
			const wrapper = createWrapper();
			const first = new File(['1'], 'first.pdf', { type: 'application/pdf' });
			const second = new File(['2'], 'second.pdf', { type: 'application/pdf' });

			vm(wrapper).onDocumentSelected([first, second]);
			await nextTick();

			expect(vm(wrapper).selectedUpload?.name).toBe('first.pdf');
			expect(vm(wrapper).selectedUpload?.type).toBe('application/pdf');
			const lastEmit = wrapper.emitted('update:selectedFile')?.at(-1)?.[0] as
				| File
				| undefined;
			expect(lastEmit?.name).toBe('first.pdf');
		});

		it('clears selected file when payload is null', async () => {
			const wrapper = createWrapper();
			const existing = new File(['x'], 'existing.pdf', {
				type: 'application/pdf',
			});

			vm(wrapper).onDocumentSelected(existing);
			await nextTick();
			vm(wrapper).onDocumentSelected(null);
			await nextTick();

			expect(vm(wrapper).selectedUpload).toBe(null);
			expect(wrapper.emitted('update:selectedFile')?.at(-1)).toEqual([null]);
		});

		it('does not update selected file when component is disabled', async () => {
			const wrapper = createWrapper({ disabled: true });
			const uploaded = new File(['x'], 'blocked.pdf', { type: 'application/pdf' });

			vm(wrapper).onDocumentSelected(uploaded);
			await nextTick();

			expect(vm(wrapper).selectedUpload).toBe(null);
			expect(wrapper.emitted('update:selectedFile')).toBeFalsy();
		});
	});

	describe('onDocumentRejected', () => {
		it('sets rejection message for one file', () => {
			const wrapper = createWrapper();
			const rejected = new File(['x'], 'malware.exe', {
				type: 'application/octet-stream',
			});

			vm(wrapper).onDocumentRejected([rejected]);

			expect(vm(wrapper).rejectedUploadMessage).toContain(
				'malware.exe is not a supported file type'
			);
		});

		it('sets rejection message for multiple files', () => {
			const wrapper = createWrapper();

			vm(wrapper).onDocumentRejected([
				new File(['x'], 'a.exe', { type: 'application/octet-stream' }),
				new File(['y'], 'b.exe', { type: 'application/octet-stream' }),
			]);

			expect(vm(wrapper).rejectedUploadMessage).toContain(
				'2 files were not supported'
			);
		});

		it('clears rejection message when no rejected files provided', () => {
			const wrapper = createWrapper();

			vm(wrapper).rejectedUploadMessage = 'Old message';
			vm(wrapper).onDocumentRejected([]);

			expect(vm(wrapper).rejectedUploadMessage).toBe('');
		});

		it('does not update rejection message when disabled', () => {
			const wrapper = createWrapper({ disabled: true });

			vm(wrapper).rejectedUploadMessage = 'Keep me';
			vm(wrapper).onDocumentRejected([
				new File(['x'], 'a.exe', { type: 'application/octet-stream' }),
			]);

			expect(vm(wrapper).rejectedUploadMessage).toBe('Keep me');
		});
	});
});
