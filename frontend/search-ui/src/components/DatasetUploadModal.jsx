import { useEffect, useState } from 'react';
import PropTypes from 'prop-types';
import { InboxOutlined } from '@ant-design/icons';
import { Form, Input, Modal, Upload, message } from 'antd';

export function DatasetUploadModal({ open, onClose, onSubmit, loading, defaultDataset }) {
  const [form] = Form.useForm();
  const [file, setFile] = useState(null);

  useEffect(() => {
    if (open) {
      form.setFieldsValue({ dataset: defaultDataset });
    } else {
      form.resetFields();
      setFile(null);
    }
  }, [open, defaultDataset, form]);

  const handleFinish = (values) => {
    if (!file) {
      message.error('Выберите CSV или JSON файл');
      return;
    }

    onSubmit({ dataset: values.dataset, file });
  };

  const uploadProps = {
    name: 'file',
    multiple: false,
    accept: '.csv,.json',
    maxCount: 1,
    beforeUpload: (selectedFile) => {
      setFile(selectedFile);
      return false;
    },
    onRemove: () => {
      setFile(null);
    }
  };

  return (
    <Modal
      title="Загрузка данных из файла"
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={loading}
      okText="Загрузить"
      cancelText="Отмена"
      destroyOnClose
    >
      <Form form={form} layout="vertical" onFinish={handleFinish}>
        <Form.Item
          label="Название датасета"
          name="dataset"
          rules={[{ required: true, message: 'Укажите название датасета' }]}
        >
          <Input placeholder="Например: crm_clients" />
        </Form.Item>
        <Form.Item label="Файл CSV или JSON">
          <Upload.Dragger {...uploadProps}>
            <p className="ant-upload-drag-icon">
              <InboxOutlined />
            </p>
            <p className="ant-upload-text">Перетащите файл или нажмите для выбора</p>
            <p className="ant-upload-hint">Поддерживаются форматы CSV и JSON</p>
          </Upload.Dragger>
        </Form.Item>
      </Form>
    </Modal>
  );
}

DatasetUploadModal.propTypes = {
  open: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  onSubmit: PropTypes.func.isRequired,
  loading: PropTypes.bool,
  defaultDataset: PropTypes.string
};

DatasetUploadModal.defaultProps = {
  loading: false,
  defaultDataset: ''
};

export default DatasetUploadModal;
